using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>
    /// Son raporunun verisi (<see cref="Simulation.EndReport"/>): yalnız kimlik, ad ve sayı; metin sunumda, gösterildiği dilde
    /// kurulur. Her okumada durumdan yeniden kurulur ve yalnız son varken döner (gizli kimlikten türeyen satırlar içerir).
    /// Gün alanları: bu dosyadaki bütün <c>Shift</c> adlı alanlar (kodun geri kalanındaki <c>DepartShift</c>, <c>OpenedShift</c>
    /// gibi) <c>S.Day</c> değeridir, 0 tabanlıdır; oyuncuya yazılan vardiya numarası bunun <b>bir fazlasıdır</b>.
    /// </summary>
    [Serializable]
    public sealed class EndReport
    {
        /// <summary>Son kimliği; ton yalnız <c>silme</c>'de ("temiz", "bedelli", "golgeli"), çeşit yalnız <c>salter</c>'de ("cekilme", "tahliye").</summary>
        public string Ending, Tone, SwitchKind;
        /// <summary>Tahliyenin nedenleri, bu sırayla: "konum", "kadro", "aclik", "guven".</summary>
        public List<string> SwitchReasons = new List<string>();
        /// <summary>Sonun geldiği gün (<c>S.Day</c>, 0 tabanlı; oyuncunun gördüğü vardiya <c>Shift + 1</c>).</summary>
        public int Shift;
        /// <summary>Zaferin ton ölçütleri sırayla (yalnız <c>silme</c>): golge, olum, aclik, ifsa, guven.</summary>
        public List<EndCriterion> Criteria = new List<EndCriterion>();

        /// <summary>Kapı: alınan aday (kurucu dışı), yollanan ve bunlardan insan, sökülen, alınmayan aday ve bunlardan sızmacı.</summary>
        public int Admitted, Sent, SentHumans, Removed, Passed, PassedInfiltrators;
        /// <summary>Ortaya çıkmamış sızmacılar (izli ya da izsiz; <see cref="EndPerson.Where"/> nerede olduklarını yazar).</summary>
        public List<EndPerson> Hidden = new List<EndPerson>();
        /// <summary>Yollanan insanlar.</summary>
        public List<EndPerson> SentHumanList = new List<EndPerson>();

        /// <summary>Kalanlar (kurtarılanlar), kayıplar ve açlıktan ayrılan ya da bırakılanlar.</summary>
        public List<EndPerson> Survivors = new List<EndPerson>();
        public List<EndPerson> Dead = new List<EndPerson>();
        public List<EndPerson> Deserters = new List<EndPerson>();

        /// <summary>İfşayı değiştiren kararlar sırayla (günlükteki "ifsa" satırları).</summary>
        public List<EndExposureStep> ExposureSteps = new List<EndExposureStep>();
        public int Exposure, Deserted, Deaths, MinTrust, RegionsFree, SectorsFree;
    }

    /// <summary>Raporda bir kişi. <see cref="Where"/>: "icerde", "oldu", "yollandi", "sokuldu", "aclik", "birakildi".</summary>
    [Serializable]
    public sealed class EndPerson
    {
        public string Id, Name, Where;
        /// <summary>Ayrılış günü (ayrılmadıysa −1) ve kabul günü (kurucuda dosyadaki değer); ikisi de <c>S.Day</c>, vardiya = +1.</summary>
        public int Shift, AcceptedShift;
        /// <summary>Dosyadaki iz sayısı ve görmezden gelinen iz kâğıdı sayısı.</summary>
        public int Clues, IgnoredTraces;
        /// <summary>Stretcher: sedyeyle (yatıyor). Infiltrator: ortaya çıkmamış sızmacı (yalnız raporda).</summary>
        public bool Founder, Stretcher, Infiltrator;
    }

    /// <summary>Zaferin ton ölçütü: değer, sınır ve tuttu mu.</summary>
    [Serializable]
    public sealed class EndCriterion
    {
        public string Id;
        public double Value, Limit;
        public bool Met;
    }

    /// <summary>İfşa değişimi: gün, kaynak ("iz", "ikinci_iz", "sabotaj", "sorgula"), kişi ve gerçek değişim.</summary>
    [Serializable]
    public sealed class EndExposureStep
    {
        /// <summary>Değişimin günü (<c>S.Day</c>, 0 tabanlı; vardiya = +1).</summary>
        public int Shift;
        public string Cause, PersonId, Name;
        public int Delta;
    }
}
