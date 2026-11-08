using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Devir fişinin verisini simülasyondan kurar (Revizyon 3 · R19c, PLAN W4; <see cref="PlanningBoardSnapshotBuilder"/> deseni).
    /// Saf okuma: Core durumunu değiştirmez, RNG çekmez, komut yazmaz; çağırıp çağırmamak oyunu değiştirmez (fişi okumak zaman
    /// ilerletmez). Yalnız public durum okunur; gizli gelecek (sıradaki gelen, adayın kimliği, ipucu ve müdahale hazırlığı) okunmaz.
    /// Görünür sözcük üretmez: veri dilden bağımsızdır, metni <c>HandoverSlipFormat</c> o anki dilde yazar.
    /// <list type="bullet">
    /// <item>Saha: <c>BoardDropRules.Occupant</c> + kişinin eşyaları yuva sırasıyla (kit, zırh, yardımcı; aynı yuvada oluşturma sırası).</item>
    /// <item>Sığınak: cep sırası (craft, cook, train), sonra BOŞTA destesi panodaki sırayla (uygunlar önce, sonra yatan ve kısıtlı;
    /// <c>PlanningBoardSnapshotBuilder</c> ile aynı); durum eki önceliği yatıyor &gt; kısıtlı &gt; yaralı (kart şeridiyle aynı).</item>
    /// <item>KAPANIŞTA BEKLENEN: erzak <c>BoardFoodPreview</c>; eğitim ve üretim Core'un kapanış kuralıyla (<c>AdvancePlanningBoard</c>,
    /// <c>AdvanceBoardProduction</c>); kapıdaki aday.</item>
    /// <item>Uyarılar: önce bu kapanışta reddedilecek kapı başvurusu, sonra DOSYA kutuları tür başına (adlar virgülle) Core'un
    /// <c>ResidentAdmissionRisk</c> sırasıyla: kısıt süresi doldu, iki bağımsız bulgu, inceleme fişi.</item>
    /// </list>
    /// Basılıyken plan kilitli olduğundan veri basılan planla aynıdır. Tahmin, kapanışın başında koşan gizli kabul olaylarını
    /// (müdahale, kapatma) bilerek bilmez: R19c "gelecekteki gizli olayı yazmaz".
    /// </summary>
    public static class HandoverSlipFactsBuilder
    {
        /// <summary>Sığınak satırlarının cep sırası (saha cepleri ayrı bölümde).</summary>
        static readonly string[] ShelterPockets = { "craft", "cook", "train" };

        /// <summary>DOSYA kutularının sırası: Core'un kabul riski sırası (<c>ResidentAdmissionRisk</c>).</summary>
        static readonly SlipWarningKind[] FileOrder =
            { SlipWarningKind.FileRestrictionExpired, SlipWarningKind.FileClues, SlipWarningKind.FileReview };

        /// <summary>Fişin verisi; simülasyon ya da kartlı pano yoksa null.</summary>
        public static HandoverSlipFacts Build(Simulation sim)
        {
            var board = sim?.Board;
            if (board == null) return null;
            var shelter = sim.Shelter;
            double supplies = shelter.Supplies;
            var f = new HandoverSlipFacts
            {
                receiptId = board.ReceiptId,
                shift = sim.S.Day + 1,
                copy = 1,
                alive = board.People.Count(p => p.Alive),
                capacity = sim.B.boardResidentCapacity,
                supplies = supplies,
                suppliesCapacity = ShelterLogic.SuppliesCapacity(shelter, sim.B),
                suppliesAfter = supplies + sim.BoardFoodPreview,
                openApplications = sim.Admission?.Candidate != null ? 1 : 0,
            };
            FillField(f, board);
            FillShelter(f, sim, board);
            FillTraining(f, sim, board);
            FillProduction(f, sim, board);
            FillWarnings(f, sim, board);
            return f;
        }

        // ---------------------------------------------------------------- saha

        static void FillField(HandoverSlipFacts f, PlanningBoardState board)
        {
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                var person = BoardDropRules.Occupant(board, role);
                var row = new SlipFieldRow { role = role, name = person?.Name };
                if (person != null)
                    foreach (var e in board.Equipment.Where(x => x.OwnerId == person.Id).OrderBy(x => SlotRank(x.Kind)))
                        row.items.Add(e.Kind);
                f.field.Add(row);
            }
        }

        /// <summary>Yuva sırası: kit, zırh, yardımcı (<c>OrderBy</c> kararlı: aynı yuvada Core'un oluşturma sırası kalır).</summary>
        static int SlotRank(string kind)
        {
            string slot = PlanningBoardData.EquipmentSlot(kind);
            return slot == "kit" ? 0 : slot == "armor" ? 1 : 2;
        }

        // ---------------------------------------------------------------- sığınak

        static void FillShelter(HandoverSlipFacts f, Simulation sim, PlanningBoardState board)
        {
            foreach (string job in ShelterPockets)
            {
                var occupant = BoardDropRules.Occupant(board, job);
                if (occupant != null) f.shelter.Add(ShelterRow(sim, occupant, job));
            }
            // BOŞTA destesi: üstte uygun kişiler (yatan ve kısıtlı olmayan), sonra kalanlar; ikisi de panodaki sırayla
            var later = new List<ResidentState>();
            foreach (var p in board.People)
            {
                if (!p.Alive || BoardDropRules.PlaceOf(board, p) != "rest") continue;
                if (p.Down <= 0 && !p.Restricted) f.shelter.Add(ShelterRow(sim, p, "rest"));
                else later.Add(p);
            }
            foreach (var p in later) f.shelter.Add(ShelterRow(sim, p, "rest"));
        }

        static SlipShelterRow ShelterRow(Simulation sim, ResidentState p, string job)
        {
            var row = new SlipShelterRow { name = p.Name, job = job, subject = job == "train" ? p.TrainingSubject : null };
            if (p.Down > 0)
            {
                row.state = SlipPersonState.Down;
                row.stateShifts = p.Down;
            }
            else if (p.Restricted)
            {
                // Kalan vardiya kart şeridindeki gibi: kısıt bitişi − bugün; 0 ya da altı "kısıtlı · bitti"
                int left = (sim.Admission?.File(p.Id)?.RestrictedUntil ?? -1) - sim.S.Day;
                row.state = left > 0 ? SlipPersonState.Restricted : SlipPersonState.RestrictionEnded;
                row.stateShifts = Math.Max(0, left);
            }
            else if (p.Hp < p.MaxHp) row.state = SlipPersonState.Hurt;
            return row;
        }

        // ---------------------------------------------------------------- KAPANIŞTA BEKLENEN

        /// <summary>
        /// Eğitim cebindeki kişi: kapanışta yatmıyorsa konusunda <c>boardTrainingGain</c> kazanır, <c>boardExpertAt</c>'ta durur
        /// (Core'un <c>GainBoardExperience</c> kuralı). Kademe Core'un <see cref="Simulation.BoardSkillTier"/>'ıyla.
        /// </summary>
        static void FillTraining(HandoverSlipFacts f, Simulation sim, PlanningBoardState board)
        {
            var trainee = BoardDropRules.Occupant(board, "train");
            if (trainee == null) return;
            var b = sim.B;
            string subject = trainee.TrainingSubject;
            int xp = trainee.Skill(subject);
            int after = trainee.Down > 0 ? xp : Math.Min(Math.Max(0, b.boardExpertAt), xp + Math.Max(0, b.boardTrainingGain));
            f.training = new SlipTraining
            {
                active = true, name = trainee.Name, subject = subject,
                xpBefore = xp, xpAfter = after,
                tierBefore = sim.BoardSkillTier(trainee, subject),
                tierAfter = sim.BoardSkillTier(WithSkill(subject, after), subject),
            };
        }

        /// <summary>Yalnız kademe hesabı için geçici kişi (simülasyona girmez).</summary>
        static ResidentState WithSkill(string subject, int points)
        {
            var p = new ResidentState();
            p.Experience.Add(new ResidentSkill { Subject = subject, Points = points });
            return p;
        }

        /// <summary>
        /// Üretim, Core'un <c>AdvanceBoardProduction</c> koşullarıyla (Aşama 5: kural tek yerde, <see cref="ProductionForecast"/>; iş
        /// emri formu aynı sonucu okur): ayakta bir üretici, açlık yok, teçhizat deposunda yer; iş emri varsa (önce gelir) başlamış ya da
        /// girdileri tam ayrılmış ve stokta, yoksa otomatik üretimin seçeceği tarif ve stok (bir kapanışta biter: "0/1 → 1/1"). Açlık
        /// temkinli okunur: kapanış erzağı 0'a iniyorsa açlık sayılır (Core'un kırpılmamış dengesi public değil; tam 0 dengesi tek fark).
        /// Fiş verisi dilden bağımsızdır: tahminin çevrilmiş durma nedeni buraya girmez.
        /// </summary>
        static void FillProduction(HandoverSlipFacts f, Simulation sim, PlanningBoardState board)
        {
            var outlook = ProductionForecast.Of(sim);
            if (outlook.IsNone) return;
            var production = f.production;
            production.itemType = outlook.Output;
            production.turns = outlook.Turns;
            production.done = outlook.Done;
            production.maker = outlook.Maker;
            production.kind = outlook.Works ? SlipProductionKind.Progress : SlipProductionKind.NoOutput;
            production.after = outlook.After;
        }

        // ---------------------------------------------------------------- uyarılar

        static void FillWarnings(HandoverSlipFacts f, Simulation sim, PlanningBoardState board)
        {
            var adm = sim.Admission;
            if (adm == null) return;
            int day = sim.S.Day;

            // KAPI: kararsız başvuru kapanışta ret sayılır; yalnız süren dış kayıt teyidi bir vardiya uzatır
            var candidate = adm.Candidate;
            if (candidate != null && day + 1 >= candidate.WaitingUntil && !RecordCheckExtends(sim, adm, candidate, day))
                f.warnings.Add(new SlipWarning { kind = SlipWarningKind.Door });

            // DOSYA: Core'un kabul riskinin türü; aynı tür tek kutuda
            var names = new Dictionary<SlipWarningKind, List<string>>();
            foreach (var p in board.People)
            {
                if (!p.Alive || sim.ResidentAdmissionRisk(p.Id) == null) continue;
                var file = adm.File(p.Id);
                if (file == null) continue;
                var kind = FileKind(adm, file, day);
                if (!names.TryGetValue(kind, out var list)) names[kind] = list = new List<string>();
                list.Add(p.Name);
            }
            foreach (var kind in FileOrder)
                if (names.TryGetValue(kind, out var list))
                    f.warnings.Add(new SlipWarning { kind = kind, name = string.Join(", ", list) });
        }

        /// <summary>
        /// Riskli dosyanın türü, Core'un sırasıyla (riskin var olduğu bilinir). Kısıt süresi doldu önce; inceleme gerekmiyorsa kalan
        /// tek risk iki bulgudur. İnceleme de gerekiyorsa Core bulguyu önce yazar, ancak bulgunun zararı gerçekleşmişse yazmaz: bunu
        /// oyuncunun dosya geçmişinde zaten gördüğü geçmiş "sabotage" kaydından okuruz (gizli bayrağa dokunmadan).
        /// </summary>
        static SlipWarningKind FileKind(CommunityAdmissionState adm, ResidentAdmissionFile file, int day)
        {
            if (file.Resident.Restricted && file.RestrictedUntil <= day) return SlipWarningKind.FileRestrictionExpired;
            if (!file.ReviewNeeded) return SlipWarningKind.FileClues;
            bool clues = file.ClueCount >= 2 && !file.Contained
                && !adm.Events.Any(e => e.PersonId == file.PersonId && e.Kind == "sabotage");
            return clues ? SlipWarningKind.FileClues : SlipWarningKind.FileReview;
        }

        /// <summary>
        /// Core'un kapanış başındaki gözlem adımı (<c>CompleteAdmissionObservation</c>): adayın dış kaydı bu vardiya gözetimdeyse
        /// karar bir vardiya uzar ve bu kapanışta ret olmaz.
        /// </summary>
        static bool RecordCheckExtends(Simulation sim, CommunityAdmissionState adm, CommunityApplicant c, int day) =>
            c.Resident != null && adm.ObservationTarget == c.Resident.Id && sim.HasAdmissionWatcher(c.Resident.Id)
            && c.Inspections.Contains("record") && c.WaitingUntil <= day + 1;
    }
}
