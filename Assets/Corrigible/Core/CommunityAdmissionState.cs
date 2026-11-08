using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    public sealed partial class PlanningBoardState
    {
        public CommunityAdmissionState Admission;
    }

    /// <summary>Belge ve görüşme; adayın oynanış değerlerinden ayrı tutulan beyanlar.</summary>
    [Serializable]
    public sealed class AdmissionDocument
    {
        public string Name, Profession, Origin, Portrait, Serial, ReferralSerial, Seal;
        public int IssuedShift;
    }

    [Serializable]
    public sealed class AdmissionProtocol
    {
        public int Version, IssuedShift;
        public string Change;
        public List<string> Rules = new List<string>();
    }

    /// <summary>Gizli kimlik yalnız üretim anında belirlenir; talimat değişince yeniden atılmaz.</summary>
    [Serializable]
    public sealed class AdmissionIdentity
    {
        internal bool Robot, PaperFault, BreathVisible;
        internal string Model, RegisteredOrigin, RegisteredSerial, RegisteredSeal;
        internal int RegisteredIssueShift;
    }

    [Serializable]
    public sealed class CommunityApplicant
    {
        public ResidentState Resident;
        public AdmissionDocument Papers;
        public int ArrivalShift, WaitingUntil;
        public List<string> Inspections = new List<string>();
        public string Interview, Breath, PaperFinding;
        public AdmissionIdentity Identity;
    }

    [Serializable]
    public sealed class ResidentAdmissionFile
    {
        public string PersonId;
        public ResidentState Resident;
        public AdmissionDocument Papers;
        public AdmissionIdentity Identity;
        public int ArrivalShift, AcceptedShift, RestrictedUntil = -1;
        public bool Founder, Departed, ReviewNeeded, RecordVerified, ReviewPending, Contained;
        public int LastReviewedClues = -1, LastObservedShift = -1;
        public int ClueCount, LastClueShift = -1;
        public string LastAccessJob, RecordFinding, Interview, Breath, PaperFinding;
        public List<string> Inspections = new List<string>();
        internal int ActiveShifts;
        internal bool Sabotaged;
    }

    [Serializable]
    public sealed class AdmissionEvent
    {
        public int Shift;
        public string PersonId, Kind, Text;
        public bool Resolved;
    }

    [Serializable]
    public sealed class CommunityAdmissionState
    {
        public CommunityApplicant Candidate;
        public AdmissionProtocol Protocol;
        public List<AdmissionProtocol> ProtocolArchive = new List<AdmissionProtocol>();
        public List<ResidentAdmissionFile> Files = new List<ResidentAdmissionFile>();
        public List<AdmissionEvent> Events = new List<AdmissionEvent>();
        public string ObservationTarget;
        public int NextArrivalShift, GeneratedApplicants;
        public ResidentAdmissionFile File(string id) => Files.Find(x => x.PersonId == id);
    }
}
