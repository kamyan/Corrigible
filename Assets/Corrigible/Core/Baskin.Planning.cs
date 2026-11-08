using System;

namespace Corrigible.Core
{
    public static partial class Baskin
    {
        /// <summary>Hazırlıkta takılan hücre, ilk belirtilen sayıda EMP atışını güçlendirir; kişisel fiziksel güç kullanılmaz.</summary>
        static int ApplyPlanningEmpBoost(BState board, Unit unit)
        {
            if (unit == null || unit.EmpBoosts <= 0) return 0;
            unit.EmpBoosts--;
            int bonus = Math.Max(0, unit.EmpBoostDamage);
            Say(board, Loc.F("EMP hücresi kullanıldı: bu atışa +{0} hasar.", bonus), "good");
            return bonus;
        }
    }
}
