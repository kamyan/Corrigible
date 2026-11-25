using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    public sealed partial class PlanningBoardState
    {
        public CommunityAdmissionState Admission;
    }

    /// <summary>Gizli kimlik yalnız grup üretiminde belirlenir; kurucuda boştur (insan, iz yok).</summary>
    [Serializable]
    public sealed class AdmissionIdentity
    {
        internal bool Robot;
        // Aday grubunun gizli iz çekimleri: üretimde bir kez çekilir, sızma zinciri ve insan izi okur.
        internal int TraceDelay, SecondTraceGap;
        internal bool HumanTrace, HumanSecondTrace;
    }

    [Serializable]
    public sealed class CommunityApplicant
    {
        public ResidentState Resident;
        public int ArrivalShift;
        public AdmissionIdentity Identity;
        /// <summary>Şüphe kademesi: 0 kefilli, 1 kayıtsız, 2 şüpheli. Görünür; sızmacı olasılığı yalnız buna bağlıdır.</summary>
        public int Tier;
        /// <summary>Şüphe satırının kimliği; <see cref="SuspicionArg"/> yalnız "kefil" satırında kefil kurucunun adıdır.</summary>
        public string SuspicionKey, SuspicionArg, Portrait;
        /// <summary>Özellik kimlikleri: kit zirh devre erzak usta saglam yarali obur acemi yavas.</summary>
        public List<string> Perks = new List<string>();
    }

    /// <summary>Alınmayan aday: geri çevrilen ("pass"), seçilmeyen ("notchosen") ya da süresi dolan ("expired").</summary>
    [Serializable]
    public sealed class PassedCandidate
    {
        public string Name, Kind;
        public int Tier, Shift;
        internal bool Robot;
    }

    [Serializable]
    public sealed class ResidentAdmissionFile
    {
        public string PersonId;
        public ResidentState Resident;
        public AdmissionIdentity Identity;
        public int ArrivalShift, AcceptedShift;
        /// <summary>Olay kısıtı <c>S.Day >= RestrictedUntil</c> olan kapanışta kalkar; kısıtsızken −1.</summary>
        public int RestrictedUntil = -1;
        public bool Founder, Departed;
        /// <summary>Dosyaya düşen iz sayısı (iz kâğıdı açıldığında artar).</summary>
        public int ClueCount;
        /// <summary>Kapıdaki kademe; kurucuda −1.</summary>
        public int Tier = -1;
        public string SuspicionKey, SuspicionArg;
        public List<string> Perks = new List<string>();
        /// <summary>Kalan taksit sayısı; kişi sığınaktan çıkınca sıfırlanır.</summary>
        public int CircuitInstallments, SupplyInstallments;
        /// <summary>Ayrılış nedeni ("yollandi", "sokuldu", "aclik", "birakildi") ve vardiyası; ayrılmamışsa null ve −1.</summary>
        public string DepartReason;
        public int DepartShift = -1;
        /// <summary>Affedilmiş (yalnız dosya notu; bir daha hırsızlık hedefi olmaz) ve ortaya çıkmış (sabotaj ya da tecrit bulgusu).</summary>
        public bool Pardoned, Revealed;
        /// <summary>Tecrit <c>S.Day >= IsolatedUntil</c> olan kapanışta biter; <c>S.Day &lt; IdleUntil</c> iken kişi görev alamaz. Yoksa −1.</summary>
        public int IsolatedUntil = -1, IdleUntil = -1;
        /// <summary>Dosyadaki izlerin yerleri ("uretim", "erzak", "nobet"), kâğıdın açıldığı sırayla.</summary>
        public List<string> TraceAreas = new List<string>();
        /// <summary>Sızma saati ve sıradaki zincir adımının vadesi. İnsan için de işler; public olsaydı izin zamanını ele verirdi.</summary>
        internal int Clock, TraceClock;
        /// <summary>Kapanan vardiyadaki görev; public olsaydı ilk izden önce kimliği ele verirdi.</summary>
        internal string LastAccessJob;
        internal bool Sabotaged, IsolationFound, FieldDecisionDue;
    }

    [Serializable]
    public sealed class CommunityAdmissionState
    {
        /// <summary>Kapıda bekleyen aday grubu; boş liste = kapıda grup yok. Hiç null olmaz.</summary>
        public List<CommunityApplicant> Group = new List<CommunityApplicant>();
        /// <summary>Grup, <c>S.Day >= GroupWaitingUntil</c> olan kapanışta gider.</summary>
        public int GroupArrivalShift = -1, GroupWaitingUntil = -1;
        public bool GroupEmergency;
        public int GeneratedGroups;
        public List<PassedCandidate> Passed = new List<PassedCandidate>();
        /// <summary>Kurucular ve alınan adaylar; yollanan, ayrılan ve ölen kişinin dosyası da kalır.</summary>
        public List<ResidentAdmissionFile> Files = new List<ResidentAdmissionFile>();
        /// <summary>Sıradaki olağan grubun vardiyası (kurulumda tek çekim) ve şimdiye dek üretilen aday sayısı.</summary>
        public int NextArrivalShift, GeneratedApplicants;
        public ResidentAdmissionFile File(string id) => Files.Find(x => x.PersonId == id);
    }
}
