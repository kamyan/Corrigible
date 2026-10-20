using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;
using Prediction = Corrigible.Ayna.Core.Prediction;

namespace Corrigible.Ayna.Mind
{
    public static class ForecastText
    {
        static readonly string[] orders = { Loc.N("Yayıl"), Loc.N("Yürü"), Loc.N("Saldır"), Loc.N("Duvar ör"), Loc.N("Eğit"), Loc.N("Üret"), Loc.N("Yeni birlik"), Loc.N("Onar"), Loc.N("Gizlen"), Loc.N("Sahte yığınak"), Loc.N("Çekirdeği taşı"), Loc.N("Tekillik başlat"), Loc.N("Bekle") };
        static readonly string[] regions = { Loc.N("Kuzey Amerika"), Loc.N("Güney Amerika"), Loc.N("Batı Avrupa"), Loc.N("Doğu Avrupa"), Loc.N("Rusya"), Loc.N("Orta Doğu"), Loc.N("Afrika"), Loc.N("Güney Asya"), Loc.N("Çin"), Loc.N("Japonya / Kore"), Loc.N("Güneydoğu Asya"), Loc.N("Okyanusya") };
        public static string Format(Prediction prediction) => Loc.F("Bu döngü {0} / {1}: %{2:0}", Loc.T(orders[(int)prediction.Kind]), prediction.Region == null ? Loc.T("genel") : Loc.T(regions[AynaWorldData.Region(prediction.Region).Key - 1]), prediction.Probability * 100);
        public static IReadOnlyList<string> TopThree(IEnumerable<Prediction> predictions) => predictions.OrderByDescending(p => p.Probability).ThenBy(p => p.Kind).ThenBy(p => p.Region, System.StringComparer.Ordinal).Take(3).Select(Format).ToArray();
        public static string SurpriseExplanation => Loc.T("Sürpriz, yeterli gözlem oluşunca ana bölgesel taahhüdün koşullu hedef olasılığıyla değerlendirilir; ödül emir bedeliyle sınırlıdır.");
    }
}
