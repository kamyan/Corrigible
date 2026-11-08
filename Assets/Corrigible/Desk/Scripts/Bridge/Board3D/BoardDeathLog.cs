using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// "Kayıp · V.nnn" için Core'a dokunmadan ölüm vardiyası (Revizyon 3 · R16, KAYIPLAR). Ölüm yalnız baskın dönüşünde olur
    /// (<c>ReturnPlanningSquad</c>); hemen ardından aynı gün baskın günlüğüne kadronun sonucu yazılır. Günlük kayıttan yeniden
    /// oynatılarak kurulduğu için bu okuma devam ettirilen oyunda da aynıdır. Saf okuma: durum değiştirmez, RNG çekmez.
    /// </summary>
    public static class BoardDeathLog
    {
        /// <summary>Kişinin öldüğü vardiya (0 tabanlı <c>S.Day</c>; gösterimde +1); kayıt yoksa null.</summary>
        public static int? DeathDay(Simulation sim, string personId)
        {
            var entry = Entry(sim, personId);
            return entry == null ? (int?)null : entry.Day;
        }

        /// <summary>Kişinin öldüğü baskının günlük satırı (bölge, sektör, gün); en yenisinden geriye aranır, yoksa null.</summary>
        public static RaidLogEntry Entry(Simulation sim, string personId)
        {
            var log = sim?.S?.RaidLog;
            if (log == null || string.IsNullOrEmpty(personId)) return null;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                var squad = log[i]?.LiberationReward?.Squad;
                if (squad == null) continue;
                for (int j = 0; j < squad.Count; j++)
                {
                    var outcome = squad[j];
                    if (outcome != null && outcome.Dead && outcome.PersonId == personId) return log[i];
                }
            }
            return null;
        }
    }
}
