using System;

namespace Corrigible.Core
{
    /// <summary>
    /// Topluluk kabulünün kurulumu ve bekleyen grubun süresi. Aday grubu, seçim ve yollama
    /// <c>Simulation.CandidateGroups.cs</c>'te; sızma zinciri ve olaylar <c>Simulation.ShelterEvents.cs</c>'te.
    /// Eski tek adaylı kabul (inceleme, belge ve dış kayıt, talimat, gözetmen, elle kısıt ve serbest yollama) aşama 7'de silindi.
    /// </summary>
    public sealed partial class Simulation
    {
        public CommunityAdmissionState Admission => Board?.Admission;

        /// <summary>Kurucu dosyaları ve ilk grubun vardiyası. Tek RNG çekimi (ilk aralık); sıra kayıt biçiminin parçasıdır.</summary>
        void SetupAdmissions()
        {
            if (Board == null || !B.planningBoardEnabled) return;
            Board.Admission = new CommunityAdmissionState { NextArrivalShift = S.Day + AdmissionInterval() };
            // Kurucu insandır ve iz bırakmaz: boş kimlik (çekimsiz). Zincir kurucuyu Founder ile atlar.
            foreach (var person in Board.People)
                Admission.Files.Add(new ResidentAdmissionFile
                {
                    PersonId = person.Id, Resident = person, Identity = new AdmissionIdentity(), Founder = true,
                    AcceptedShift = S.Day, ArrivalShift = S.Day,
                });
        }

        int AdmissionInterval()
        {
            int min = Math.Max(1, B.admissionKnockMin), max = Math.Max(min, B.admissionKnockMax);
            return min + (int)Math.Floor(S.Rng.Next() * (max - min + 1));
        }

        /// <summary>
        /// Gün arttıktan sonra, pano kapanışından önce çağrılır: bekleyen grup <c>S.Day >= GroupWaitingUntil</c> olan kapanışta
        /// cezasız gider. Yeni grup burada üretilmez (bkz. <c>AdvanceCandidateDoor</c>, pano kapanışının arkasında). Çekim yok.
        /// </summary>
        void AdvanceAdmissions()
        {
            if (Admission == null) return;
            if (Admission.Group.Count > 0 && S.Day >= Admission.GroupWaitingUntil) ClearCandidateGroup("expired");
        }
    }
}
