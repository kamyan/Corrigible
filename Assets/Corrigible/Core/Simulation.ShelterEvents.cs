using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Sığınak olayları: kapanışta public durumdan tetiklenir, aynı anda en çok biri açıktır ve fişten önce karara bağlanır.
    /// Rastgelelik yalnız kapanışta çekilir; çekim sayısı gizli kimliğe ya da bir olasılık değerine bağlı değildir.
    /// Komutlar, denetimler ve okuyucular çekim yapmaz.
    /// </summary>
    public sealed partial class Simulation
    {
        public ShelterEventState ShelterEvents => Board?.ShelterEvents;

        /// <summary>Karar bekleyen olay; yoksa null.</summary>
        public ShelterEventInstance PendingShelterEvent
        {
            get
            {
                var e = ShelterEvents?.Pending;
                return e != null && !string.IsNullOrEmpty(e.Kind) ? e : null;
            }
        }

        bool ShelterEventsOn => B.shelterEventsEnabled && ShelterEvents != null && Admission != null;

        /// <summary>Kapanışın yazılacağı gün: kapanış sürerken bugün, plan sırasında yarın.</summary>
        int BoardCloseDay => Board != null && Board.Filing ? S.Day : S.Day + 1;

        /// <summary>Bu vardiyanın kapanışı yarım tayınla mı yapılacak.</summary>
        public bool HalfRationActive => ShelterEventsOn && BoardCloseDay <= Shelter.HalfRationUntil;

        /// <summary>Yarım tayınla yapılacak kalan kapanış sayısı (bu vardiyanınki dahil).</summary>
        public int HalfRationShiftsLeft => HalfRationActive ? Shelter.HalfRationUntil - BoardCloseDay + 1 : 0;

        /// <summary>Bu vardiyanın kapanışı karartmada mı (Üretim ve Mutfak çalışmaz).</summary>
        public bool BlackoutActive => ShelterEventsOn && Shelter.BlackoutShift == BoardCloseDay;

        /// <summary>
        /// Boş tecrit yeri. Ortaya çıkmış sızmacı (sabotaj kâğıdının kararını bekleyen ya da sorgulanan) yer tutmaz: zaten yer
        /// tutmadan bekletiliyor (karar 14 Kasım 2026, ikinci tur).
        /// </summary>
        public int IsolationFree => Board == null ? 0
            : Math.Max(0, B.isolationCapacity - Board.People.Count(p => p.Alive && p.Isolated && HoldsIsolationRoom(Admission?.File(p.Id))));

        static bool HoldsIsolationRoom(ResidentAdmissionFile f) => f != null && f.IsolatedUntil >= 0 && !f.Revealed;

        /// <summary>
        /// Aç sığınaktan ayrılmalar sürüyor: son kapanış aç geçti, açlık sayacı ayrılma eşiğinde ve bu kapanış da aç geçerse
        /// ayrılacak kişi belli (<see cref="NextDeserter"/>). Tok kapanıştan sonra (sayaç eşikte kalsa da) sürmüyor sayılır. Son
        /// şalterin "aclik" tahliye nedeni. Public durumdan türer, çekim yapmaz.
        /// </summary>
        public bool DesertionsOngoing => ShelterEventsOn && B.desertStreak > 0 && Shelter.Starving && Shelter.HungryStreak >= B.desertStreak
            && NextDeserter != null;

        /// <summary>Bu kapanış da aç geçerse ayrılacak kişi (fiş satırı); yoksa null.</summary>
        public ResidentState NextDeserter => !ShelterEventsOn || B.desertStreak <= 0 || Shelter.HungryStreak + 1 < B.desertStreak
            ? null : DeserterCandidate(S.Day + 1);

        void SetupShelterEvents()
        {
            if (Board == null) return;
            Board.ShelterEvents = new ShelterEventState { NextEventShift = S.Day + Math.Max(1, B.eventGapMin) };
        }

        int BoardAliveCount => Board.People.Count(p => p.Alive);
        bool InShelter(string id) { var p = id == null ? null : Board.Person(id); return p != null && p.Alive; }

        /// <summary>Olay hedefi olabilecek kişi: ayakta, kısıtsız, tecrit dışında, dosyası var. Kimlik gözetilmez.</summary>
        bool EventEligible(ResidentState p)
        {
            if (p == null || !p.Alive || p.Down > 0 || p.Restricted || p.Isolated) return false;
            var f = Admission.File(p.Id);
            return f != null && !f.Departed;
        }

        string EventPersonName(string id) => id == null ? "" : Board.Person(id)?.Name ?? Admission?.File(id)?.Resident?.Name ?? id;

        void LogShelterEvent(string kind, string option = null, string personId = null, string otherId = null, double amount = 0)
            => ShelterEvents.Log.Add(new ShelterEventLogEntry { Shift = S.Day, Kind = kind, Option = option, PersonId = personId, OtherId = otherId, Amount = amount });

        ShelterEventCount EventCount(string kind)
        {
            var c = ShelterEvents.Count(kind);
            if (c == null) { c = new ShelterEventCount { Kind = kind }; ShelterEvents.Counts.Add(c); }
            return c;
        }

        /// <summary>Tekrar sınırı ve bekleme; zincir olaylarında sınır yapısaldır (kişi başı bir).</summary>
        void EventLimit(string kind, out int max, out int cooldown)
        {
            switch (kind)
            {
                case "buyuk_hirsizlik": max = B.eventBigTheftMax; cooldown = B.eventBigTheftCooldown; break;
                case "kacak": max = B.eventDeserterMax; cooldown = B.eventDeserterCooldown; break;
                case "erzak_azaliyor": max = B.eventLowRationMax; cooldown = B.eventLowRationCooldown; break;
                case "hirsizlik": max = B.eventTheftMax; cooldown = B.eventTheftCooldown; break;
                case "agir_yarali": max = B.eventWoundedMax; cooldown = B.eventWoundedCooldown; break;
                case "kavga": max = B.eventFeudMax; cooldown = B.eventFeudCooldown; break;
                case "anma": max = B.eventMemorialMax; cooldown = B.eventMemorialCooldown; break;
                case "karartma": max = B.eventBlackoutMax; cooldown = B.eventBlackoutCooldown; break;
                case "takas": max = B.eventTradeMax; cooldown = B.eventTradeCooldown; break;
                case "cagri": max = B.eventCallMax; cooldown = B.eventCallCooldown; break;
                case "konvoy": max = B.eventConvoyMax; cooldown = B.eventConvoyCooldown; break;
                default: max = int.MaxValue; cooldown = 0; break;
            }
        }

        bool EventAllowed(string kind)
        {
            EventLimit(kind, out int max, out int cooldown);
            var c = ShelterEvents.Count(kind);
            if (c == null) return max > 0;
            return c.Count < max && (c.LastShift < 0 || S.Day >= c.LastShift + Math.Max(0, cooldown));
        }

        /// <summary>
        /// İfşa sayacını değiştirir: sıfırın altına inmez, tavanı yoktur (eşikteki sabotaj da +2 yazar; kötü olay sayacı hiç
        /// geri çekmez). Günlüğe kaynak (<paramref name="cause"/>: "iz", "ikinci_iz", "sabotaj", "sorgula") ve kişi yazılır; miktar
        /// gerçek değişimdir. Sayaç eşiğe varırsa uyarı süresi yazılır (<see cref="ShelterState.FallGrace"/>): kapanışta açılan
        /// sabotajla varıldıysa bir vardiya, vardiya içindeki bir kararla (görmezden gelinen iz) varıldıysa
        /// <c>exposureDecisionWarnShifts</c>. Sayaç eşiğin altına inerse "konum çözüldü" uyarısı o anda kalkar (<c>konum_kalkti</c>):
        /// uyarı vardiyasında sorgulanan sığınak artık düşmeyecektir, aynı kapanışta açılan yeni bir sabotaj ancak yeni bir uyarı kurar.
        /// Eşik ve son kapanışta sınanır (<see cref="AdvanceShelterEndings"/>).
        /// </summary>
        void ChangeExposure(int delta, string cause, string personId)
        {
            if (delta == 0) return;
            int before = Shelter.Exposure;
            int after = Math.Max(0, Shelter.Exposure + delta);
            Shelter.Exposure = after;
            LogShelterEvent("ifsa", cause, personId, null, after - before);
            bool below = B.exposureThreshold <= 0 || after < B.exposureThreshold;
            if (!below && before < B.exposureThreshold)
                // Yalnız sabotaj kapanışta gelir; öteki kaynaklar vardiya içindeki kararlardır.
                Shelter.FallGrace = cause == "sabotaj" ? 0 : Math.Max(1, B.exposureDecisionWarnShifts) - 1;
            if (below) Shelter.FallGrace = 0;
            if (Shelter.FallPending && below)
            {
                Shelter.FallPending = false;
                LogShelterEvent("konum_kalkti");
            }
        }

        // ---------- Kapanış: kişiler (çekim yok) ----------
        /// <summary>Kavga süreleri ve sızma saati. Pano işleri ödenmeden önce, kapanan vardiyanın görevleriyle çalışır.</summary>
        void AdvanceShelterPeople()
        {
            if (!ShelterEventsOn) return;
            ShelterEvents.Feuds.RemoveAll(x => S.Day >= x.Until);
            foreach (var f in Admission.Files)
            {
                if (f.Departed || !f.Resident.Alive) continue;
                // Saat görev ayırmaz (Boşta ve Eğitim de işler); yalnız yatış, tecrit ve açılmayı bekleyen zincir adımı durdurur.
                if (f.Founder || f.Resident.Down > 0 || f.Resident.Isolated || ChainDue(f) != null) continue;
                f.LastAccessJob = f.Resident.Job;
                f.Clock++;
            }
        }

        /// <summary>
        /// Vadesi gelmiş ama kâğıdı açılmamış zincir adımı ("iz" | "ikinci_iz" | "sabotaj"); yoksa null. Çekim yapmaz: bütün
        /// rastgelelik aday üretiminde çekilmiştir.
        /// </summary>
        string ChainDue(ResidentAdmissionFile f)
        {
            if (f.Founder || f.Departed || !f.Resident.Alive || f.Sabotaged || f.Resident.Isolated || f.Identity == null) return null;
            var id = f.Identity;
            bool active = id.Robot || f.ClueCount == 0 && id.HumanTrace || f.ClueCount == 1 && id.HumanTrace && id.HumanSecondTrace;
            if (!active || f.Clock < f.TraceClock) return null;
            return f.ClueCount == 0 ? "iz" : f.ClueCount == 1 ? "ikinci_iz" : f.ClueCount == 2 && id.Robot ? "sabotaj" : null;
        }

        static string TraceArea(string job)
        {
            switch (job)
            {
                case "craft": case "tek": case "emp": case "ana": return "uretim";
                case "cook": return "erzak";
                default: return "nobet";
            }
        }

        // ---------- Kapanış: açlıktan ayrılma (çekim yok) ----------
        /// <summary>
        /// Ayrılacak kişi: kurucu olmayan ve rol kiti taşımayan en yeni kişi önce, kit taşıyan kurucu en son. Yatan, tecritteki,
        /// kısıtlı ve bu vardiya katılan kişi atlanır; son kişi gitmez. Sıra yalnız public alanlara bakar.
        /// </summary>
        ResidentState DeserterCandidate(int closeDay)
        {
            if (Admission == null || BoardAliveCount <= 1) return null;
            var files = Admission.Files;
            return files
                .Where(f => !f.Departed && f.Resident.Alive && Board.People.Contains(f.Resident) && f.Resident.Down <= 0
                    && !f.Resident.Isolated && !f.Resident.Restricted && f.AcceptedShift < closeDay - 1)
                .OrderBy(f => f.Founder ? 1 : 0)
                .ThenBy(f => Board.Equipment.Any(e => e.OwnerId == f.PersonId && e.Charges > 0 && PlanningBoardData.EquipmentSlot(e.Kind) == "kit") ? 1 : 0)
                .ThenByDescending(f => f.AcceptedShift)
                .ThenByDescending(f => files.IndexOf(f))
                .Select(f => f.Resident).FirstOrDefault();
        }

        /// <summary>Aç kapanışta, sayaç eşiğe ulaştıysa bir kişi ayrılır. Pano kapanışının içinde, kişi döngüsünden önce çağrılır.</summary>
        void DesertFromHunger()
        {
            if (!ShelterEventsOn || B.desertStreak <= 0 || !Shelter.Starving || Shelter.HungryStreak < B.desertStreak) return;
            var person = DeserterCandidate(S.Day);
            if (person == null) return;
            SendAway(Admission.File(person.Id), "aclik", false);
            Shelter.Deserted++;
            Shelter.DesertedSinceFull = true;
            LogShelterEvent("ayrilma", null, person.Id);
        }

        // ---------- Kapanış: vadeli sonuçlar ve tecrit bitişi ----------
        /// <summary>
        /// (a) Çekilmemiş her vadeli kayıt için karar sırasıyla iki çekim (olur mu; ne zaman ya da kim). (b) Vadesi gelenler
        /// uygulanır. (c) Tecridi biten her kişi için dosya sırasıyla tek çekim (ortaya çıkmış kişi çekimsiz sökülür).
        /// Çekimler olasılık değerine ya da kimliğe göre atlanmaz; kullanılmayan atılır.
        /// </summary>
        void AdvanceShelterDelayed()
        {
            if (!ShelterEventsOn)
            {
                if (B.infiltratorFieldEnabled && Admission != null)
                    foreach (var f in Admission.Files.Where(f => f.Revealed && f.Resident.Alive && f.Resident.Isolated &&
                        f.IsolatedUntil >= 0 && S.Day >= f.IsolatedUntil).ToArray())
                    {
                        SendAway(f, "sokuldu", false);
                        LogShelterEvent("sokuldu", null, f.PersonId);
                    }
                return;
            }
            // Kısıt, süresinin son kapanışı bittikten sonra kalkar: o kapanışın açlıktan ayrılması kişiyi hâlâ kısıtlı görür; fişin
            // "ayrılır" satırı (NextDeserter) da planda onu kısıtlı gördüğü için ikisi aynı kişiyi yazar. Çekim yok.
            foreach (var f in Admission.Files)
            {
                if (f.Departed || !f.Resident.Alive || !f.Resident.Restricted || S.Day < f.RestrictedUntil) continue;
                f.Resident.Restricted = false;
                f.RestrictedUntil = -1;
            }
            var delayed = ShelterEvents.Delayed;
            foreach (var d in delayed)
            {
                if (d.Drawn) continue;
                double u = S.Rng.Next(), v = S.Rng.Next();
                d.Drawn = true;
                d.Pick = v;
                d.DueShift = S.Day;
                switch (d.Kind)
                {
                    case "buyuk_hirsizlik":
                        d.Occurs = u < B.bigTheftChance;
                        int min = Math.Max(1, B.bigTheftDelayMin), max = Math.Max(min, B.bigTheftDelayMax);
                        d.DueShift = d.DecidedShift + min + Pick(v, max - min + 1);
                        break;
                    case "yatis_uzar": d.Occurs = u < B.woundWorsenChance; break;
                    case "kavga_yara": d.Occurs = u < B.feudInjuryChance; break;
                    case "yas": d.Occurs = u < B.griefChance; break;
                    case "cagri_guven": d.Occurs = u < B.callRefuseChance; break;
                }
            }
            foreach (var d in delayed.ToArray())
            {
                if (!d.Drawn || d.Ready) continue;
                if (!d.Occurs) { delayed.Remove(d); continue; }
                if (S.Day < d.DueShift) continue;
                // Büyük hırsızlık kâğıt olarak açılır (olay adımında); diğerleri burada uygulanır.
                if (d.Kind == "buyuk_hirsizlik") { d.Ready = true; continue; }
                delayed.Remove(d);
                ApplyShelterDelayed(d);
            }
            foreach (var f in Admission.Files.ToArray())
            {
                if (f.Departed || !f.Resident.Alive || !f.Resident.Isolated || f.IsolatedUntil < 0 || S.Day < f.IsolatedUntil) continue;
                if (f.Revealed)
                {
                    // Sorgu bitti: ortaya çıkmış sızmacı çekimsiz sökülür.
                    SendAway(f, "sokuldu", false);
                    LogShelterEvent("sokuldu", null, f.PersonId);
                    continue;
                }
                double u = S.Rng.Next();
                double chance = f.ClueCount >= 2 ? B.isolationFindChanceTwo : B.isolationFindChance;
                if (f.Identity != null && f.Identity.Robot && u < chance)
                {
                    f.IsolationFound = true;
                    f.Revealed = true;
                    SendAway(f, "sokuldu", false);
                    LogShelterEvent("bulgu", null, f.PersonId);
                }
                else
                {
                    // "Bulgu yok" kimseyi temize çıkarmaz: izler dosyada kalır, saat kaldığı yerden sürer.
                    f.Resident.Isolated = false;
                    f.IsolatedUntil = -1;
                    LogShelterEvent("bulgu_yok", null, f.PersonId);
                }
            }
        }

        void ApplyShelterDelayed(ShelterDelayed d)
        {
            switch (d.Kind)
            {
                case "yatis_uzar":
                {
                    // Karar yatış sürerken verilir ve çekim bir sonraki kapanıştadır: o kapanış yatışı bitirmiş olsa da kişi bir vardiya daha yatar.
                    var p = Board.Person(d.PersonId);
                    if (p == null || !p.Alive) return;
                    p.Down += 1;
                    p.Job = "rest";
                    LogShelterEvent("vadeli", d.Kind, p.Id);
                    break;
                }
                case "kavga_yara":
                {
                    var p = Board.Person(Pick(d.Pick, 2) == 0 ? d.PersonId : d.OtherId);
                    if (p == null || !p.Alive) return;
                    p.Hp = Math.Max(1, p.Hp - 1);
                    p.Down = Math.Max(p.Down, 1);
                    p.Job = "rest";
                    LogShelterEvent("vadeli", d.Kind, p.Id);
                    break;
                }
                case "yas":
                {
                    if (d.Mates.Count == 0) return;
                    var p = Board.Person(d.Mates[Pick(d.Pick, d.Mates.Count)]);
                    var f = p == null ? null : Admission.File(p.Id);
                    if (p == null || !p.Alive || f == null) return;
                    f.IdleUntil = Math.Max(f.IdleUntil, S.Day + 1);
                    p.Job = "rest";
                    LogShelterEvent("vadeli", d.Kind, p.Id);
                    break;
                }
                case "cagri_guven":
                    // Gecikmeli güven kaybı güveni 1'in altına indirmez.
                    if (S.Trust <= 1) return;
                    ChangeTrust(null, -1);
                    LogShelterEvent("vadeli", d.Kind, null, null, -1);
                    break;
            }
        }

        // ---------- Kapanış: olay açılışı ----------
        RaidLogEntry ClosedShiftRaid
        {
            get
            {
                var last = S.RaidLog.Count > 0 ? S.RaidLog[S.RaidLog.Count - 1] : null;
                return last != null && last.Day == S.Day - 1 && last.LiberationReward != null ? last : null;
            }
        }

        /// <summary>
        /// Sıra: zincir (dosya sırasıyla ilk vadesi gelen; aralık dışı), sonra aralık dolduysa olağan olaylar (öncelik sırasıyla ilk
        /// tetiklenen; büyük hırsızlık "kişiler" önceliğinin başındadır). Yalnız olağan açılış çekim yapar: aralık ve hedef
        /// (hedefsiz olayda hedef çekimi atılır).
        /// </summary>
        void OpenShelterEvent()
        {
            OpenFieldDecision();
            if (!ShelterEventsOn || S.Ending != null || PendingShelterEvent != null || BoardAliveCount == 0) return;
            var state = ShelterEvents;
            state.Pending = null;

            foreach (var f in Admission.Files)
            {
                string due = ChainDue(f);
                if (due == null) continue;
                OpenChainEvent(f, due);
                return;
            }

            // Vadesi gelmiş büyük hırsızlık: kişi gitmişse, çalınacak erzak yoksa ya da sınır dolmuşsa düşer, ertelenmez. Kişi
            // yatıyor, kısıtlı ya da tecritteyse veya aralık dolmadıysa bekler (çalamayacak durumdaki kişi çalmaz).
            EventLimit("buyuk_hirsizlik", out int bigTheftMax, out _);
            int bigThefts = ShelterEvents.Count("buyuk_hirsizlik")?.Count ?? 0;
            state.Delayed.RemoveAll(d => d.Ready && (!InShelter(d.PersonId) || Shelter.Supplies < 1 || bigThefts >= bigTheftMax));

            if (S.Day < state.NextEventShift) return;
            string kind = null;
            foreach (string k in ShelterEventData.Ordinary)
                if (EventAllowed(k) && OrdinaryEventTrigger(k)) { kind = k; break; }
            if (kind == null) return;

            double uGap = S.Rng.Next(), uTarget = S.Rng.Next();
            var c = EventCount(kind);
            var e = new ShelterEventInstance { Kind = kind, OpenedShift = S.Day, PrevNextEventShift = state.NextEventShift, PrevLastShift = c.LastShift };
            int gapMin = Math.Max(1, B.eventGapMin), gapMax = Math.Max(gapMin, B.eventGapMax);
            state.NextEventShift = S.Day + gapMin + Pick(uGap, gapMax - gapMin + 1);
            c.Count++;
            c.LastShift = S.Day;
            switch (kind)
            {
                case "buyuk_hirsizlik":
                {
                    // Hedef RNG'siz: karar sırasıyla ilk hazır kayıt (hedef çekimi atılır).
                    var d = ReadyBigTheft();
                    state.Delayed.Remove(d);
                    e.PersonId = d.PersonId;
                    e.Amount = Math.Min(Math.Max(0, B.bigTheftSupplies), Shelter.Supplies);
                    e.Resource = "erzak";
                    Shelter.Supplies -= e.Amount;
                    break;
                }
                case "kacak":
                {
                    var pool = Board.People.Where(EventEligible).ToList();
                    e.PersonId = pool[Pick(uTarget, pool.Count)].Id;
                    break;
                }
                case "hirsizlik":
                {
                    var pool = Board.People.Where(p => EventEligible(p) && !Admission.File(p.Id).Pardoned).ToList();
                    e.PersonId = pool[Pick(uTarget, pool.Count)].Id;
                    e.Amount = Math.Min(Math.Max(0, B.theftSupplies), Shelter.Supplies);
                    e.Resource = "erzak";
                    Shelter.Supplies -= e.Amount;
                    break;
                }
                case "agir_yarali":
                {
                    var p = WoundedEventTarget();
                    e.PersonId = p.Id;
                    e.Amount = p.Down;
                    break;
                }
                case "kavga":
                {
                    var pair = FeudEventPair();
                    e.PersonId = pair[0];
                    e.OtherId = pair[1];
                    break;
                }
                case "anma":
                    e.PersonId = ClosedShiftRaid.LiberationReward.Squad.First(x => x.Dead).PersonId;
                    break;
            }
            state.Pending = e;
        }

        void OpenFieldDecision()
        {
            if (!B.infiltratorFieldEnabled || ShelterEvents == null || PendingShelterEvent != null) return;
            var f = Admission?.Files.FirstOrDefault(v => v.FieldDecisionDue && !v.Departed && v.Resident.Alive);
            if (f == null) return;
            f.FieldDecisionDue = false;
            OpenChainEvent(f, "sabotaj", fieldCapture: true);
        }

        void OpenChainEvent(ResidentAdmissionFile f, string kind, bool fieldCapture = false)
        {
            var state = ShelterEvents;
            var count = EventCount(kind);
            var e = new ShelterEventInstance
            {
                Kind = kind, PersonId = f.PersonId, OpenedShift = S.Day,
                PrevNextEventShift = state.NextEventShift, PrevLastShift = count.LastShift,
            };
            count.Count++;
            count.LastShift = S.Day;
            if (fieldCapture)
            {
                e.Area = "saha";
            }
            else if (kind == "sabotaj")
            {
                string job = f.LastAccessJob;
                if (job == "craft" || PlanningBoardData.FieldJobs.Contains(job))
                {
                    int loss = Math.Min(BoardMaterialFree("metal"), Math.Max(0, B.admissionSabotageMaterials));
                    var metal = Board.Material("metal");
                    if (metal != null && loss > 0) { metal.Quantity -= loss; SyncBoardMaterialTotal(); }
                    e.Amount = loss;
                    e.Resource = "metal";
                }
                else
                {
                    double loss = Math.Min(Shelter.Supplies, Math.Max(0, B.admissionSabotageRations));
                    Shelter.Supplies -= loss;
                    e.Amount = loss;
                    e.Resource = "erzak";
                }
                f.Sabotaged = true;
                f.Revealed = true;
                ChangeExposure(B.exposureSabotage, "sabotaj", f.PersonId);
                // Ortaya çıkan kişi kararına kadar tecrit yerinde, yer tutmadan bekletilir (IsolatedUntil −1).
                f.Resident.Isolated = true;
                f.IsolatedUntil = -1;
                f.Resident.Job = "rest";
                ReleaseResidentEquipment(f.PersonId, false);
            }
            else
            {
                // İz, kâğıdı açıldığında dosyaya düşer; public durum kâğıttan önce ipucu vermez.
                e.Area = TraceArea(f.LastAccessJob);
                f.ClueCount++;
                f.TraceAreas.Add(e.Area);
                f.TraceClock = f.Clock + (kind == "iz"
                    ? Math.Max(1, f.Identity.SecondTraceGap)
                    : 1 + Math.Max(0, B.interventionShifts));
            }
            state.Pending = e;
        }

        /// <summary>Olağan olayın tetiği; yalnız public ve kayıtlı duruma bakar, çekim yapmaz.</summary>
        bool OrdinaryEventTrigger(string kind)
        {
            double low = Math.Max(0, B.rationWarnShifts) * BoardConsumption;
            var raid = ClosedShiftRaid;
            switch (kind)
            {
                case "buyuk_hirsizlik":
                    return ReadyBigTheft() != null;
                case "kacak":
                    return Shelter.Starving && Shelter.HungryStreak >= B.eventDeserterStreak && Board.People.Any(EventEligible);
                case "erzak_azaliyor":
                    return Shelter.Supplies < low && !HalfRationActive;
                case "karartma":
                {
                    // Oyuncunun yeni vardiyada göreceği alarmla sınanır (bu kapanışın alarm adımı olay açılışından sonradır).
                    double alarm = S.Alarm + AlarmPerShift;
                    return alarm >= B.liberationAlarmThreshold - B.blackoutAlarmMargin && alarm < B.liberationAlarmThreshold;
                }
                case "hirsizlik":
                    return (HalfRationActive || Shelter.Supplies < low) && Shelter.Supplies >= B.theftSupplies && B.theftSupplies > 0
                        && Board.People.Any(p => EventEligible(p) && !Admission.File(p.Id).Pardoned);
                case "agir_yarali":
                    return WoundedEventTarget() != null;
                case "kavga":
                    return raid != null && (!raid.Success || raid.LiberationReward.Squad.Any(x => x.Dead)) && FeudEventPair() != null;
                case "anma":
                    return raid != null && raid.LiberationReward.Squad.Any(x => x.Dead && x.PersonId != null);
                case "takas":
                    return Shelter.Supplies < B.tradeLowSupplies && BoardMaterialFree("metal") >= B.tradeMetal
                        || PlanningBoardData.FieldJobs.Any(r => BoardKitMissing("kit_" + r)) && Shelter.Supplies > B.tradeHighSupplies;
                case "cagri":
                    return raid != null && raid.Success && Shelter.Supplies > B.callMinSupplies;
                case "konvoy":
                    return S.Trust >= B.convoyTrustAt;
                default:
                    return false;
            }
        }

        /// <summary>Açılabilecek ilk vadesi gelmiş büyük hırsızlık kaydı (karar sırasıyla; kişisi olay hedefi olabilir); yoksa null.</summary>
        ShelterDelayed ReadyBigTheft()
            => ShelterEvents.Delayed.FirstOrDefault(d => d.Ready && Shelter.Supplies >= 1 && EventEligible(Board.Person(d.PersonId)));

        /// <summary>
        /// Kapanan vardiyanın baskınında düşüp kurtarılan ve yatışı en az iki vardiya olan ilk kişi (kadro sırasıyla). Olay yatışın
        /// başladığı kapanışta sınanır: pano kapanışı yatışı bir azaltmıştır, bu yüzden kalan yatış en az 1 ise başlangıçta en az 2'ydi
        /// (kendi alanında eğitimli saha kişisi 2 vardiya yatar).
        /// </summary>
        ResidentState WoundedEventTarget()
        {
            var raid = ClosedShiftRaid;
            if (raid == null) return null;
            foreach (var o in raid.LiberationReward.Squad)
            {
                if (!o.Down || o.Dead) continue;
                var p = Board.Person(o.PersonId);
                if (p != null && p.Alive && p.Down >= 1) return p;
            }
            return null;
        }

        /// <summary>Kapanan vardiyanın baskın kadrosundan, sığınakta ve sağ olan ilk iki kişi; ikisi yoksa null.</summary>
        string[] FeudEventPair()
        {
            var raid = ClosedShiftRaid;
            if (raid == null) return null;
            var ids = raid.LiberationReward.Squad.Where(x => !x.Dead && InShelter(x.PersonId)).Select(x => x.PersonId).Distinct().Take(2).ToArray();
            return ids.Length == 2 ? ids : null;
        }

        // ---------- Okuyucular ----------
        /// <summary>Açık olayın o dildeki gövde metni; olay yoksa null.</summary>
        public string ShelterEventBody(ShelterEventInstance e)
        {
            if (e == null || Board == null) return null;
            string name = EventPersonName(e.PersonId);
            switch (e.Kind)
            {
                case "iz":
                    return e.Area == "uretim" ? Loc.F("{0}: üretim dolabında yetkisiz erişim kaydı var.", name)
                        : e.Area == "erzak" ? Loc.F("{0}: erzak sayımı onun nöbetinde tutmuyor.", name)
                        : Loc.F("{0}: gece yerinde değildi; telsiz odasının kapısı açık bulundu.", name);
                case "ikinci_iz": return Loc.F("{0}: ikinci iz. Bakım defteriyle uyuşmayan bir parça bulundu.", name);
                case "sabotaj":
                    if (e.Area == "saha") return Loc.F("{0} baskında taraf değiştirdi. Tecritte kararınızı bekliyor.", name);
                    return e.Resource == "metal"
                        ? Loc.F("{0} sabotaj yaptı ve ortaya çıktı: {1} metal kaybedildi.", name, (int)e.Amount)
                        : Loc.F("{0} sabotaj yaptı ve ortaya çıktı: {1:0.##} erzak kaybedildi.", name, e.Amount);
                case "buyuk_hirsizlik": return Loc.F("{0} yeniden çaldı: {1:0.##} erzak eksik.", name, e.Amount);
                case "kacak": return Loc.F("{0} gitmek istiyor: “Burada açlıktan öleceğiz.”", name);
                case "erzak_azaliyor": return Loc.F("Erzak {0} vardiyalık tüketimin altına indi.", Math.Max(0, B.rationWarnShifts));
                case "hirsizlik": return Loc.F("{0} depodan {1:0.##} erzak aldı.", name, e.Amount);
                case "agir_yarali": return Loc.F("{0} ağır yaralı döndü: {1} vardiya yatacak.", name, (int)e.Amount);
                case "kavga": return Loc.F("{0} ile {1} baskın dönüşü birbirine girdi.", name, EventPersonName(e.OtherId));
                case "anma": return Loc.F("{0} baskında kaybedildi. Sığınak onu anmak istiyor.", name);
                case "karartma": return Loc.T("YZ alarmı eşiğe yaklaştı. Makineler susturulabilir.");
                case "takas": return Loc.T("Kapıda bir takasçı var: erzak, metal ve devre değiş tokuş ediyor.");
                case "cagri": return Loc.T("Özgür bölgeden çağrı geldi: erzak istiyorlar.");
                case "konvoy": return Loc.T("Halk bir erzak konvoyu yollamak istiyor; konvoy dikkat çeker.");
                default: return e.Kind;
            }
        }

        /// <summary>
        /// Açık olayın seçenekleri, o dilde: ad, anlık bedel, bilinen gecikmeli risk (olasılığın sayısı yazılmaz), açıklık ve
        /// kapalıysa gerekçe. Olay yoksa boş liste. Hiçbir seçenek gizli kimliğe göre metin, bedel ya da kapalılık değiştirmez.
        /// </summary>
        public List<ShelterEventOption> ShelterEventOptions()
        {
            var list = new List<ShelterEventOption>();
            var e = PendingShelterEvent;
            if (e == null) return list;
            string safe = ShelterEventData.SafeOption(e.Kind);
            foreach (string id in ShelterEventData.Options(e.Kind))
            {
                ShelterOptionText(e, id, out string cost, out string risk);
                string closed = ShelterOptionClosed(e, id);
                list.Add(new ShelterEventOption
                {
                    Id = id, Name = ShelterEventData.OptionName(e.Kind, id), Cost = cost, Risk = risk,
                    KitLock = KeepsPersonOnBoard(id) ? KitLockLine(e.PersonId) : null,
                    Open = closed == null, ClosedReason = closed, Safe = id == safe,
                });
            }
            return list;
        }

        /// <summary>Seçeneğin kapalılık gerekçesi; açıksa null. Yalnız public duruma bakar.</summary>
        string ShelterOptionClosed(ShelterEventInstance e, string option)
        {
            bool last = BoardAliveCount <= 1;
            switch (option)
            {
                case "tecrit":
                    // Tecrit bulguyla kişi çıkarabilir: kişi çıkaran seçenek gibi son kişide kapalıdır (yoksa bulgu sığınağı boşaltır).
                    if (last) return Loc.T("Son kişi tecrit edilemez: sığınakta başka kimse yok.");
                    return IsolationFree > 0 ? null : Loc.T("Tecrit yeri dolu.");
                case "sorgula":
                    // Ortaya çıkmış sızmacı zaten yer tutmadan bekletiliyor: sorgu tecrit yeri istemez (ikinci tur kararı).
                    return null;
                case "yolla": case "sok": case "birak":
                    return last ? Loc.T("Son kişi sığınaktan çıkarılamaz.") : null;
                case "kilitle":
                    return S.Trust > 1 ? null : Loc.T("Güven çok düşük.");
                case "kaldir":
                    return Shelter.Supplies >= B.reviveSupplies ? null : Loc.F("{0:0.##} erzak gerekiyor.", B.reviveSupplies);
                case "an":
                    return Shelter.Supplies >= B.memorialSupplies ? null : Loc.F("{0:0.##} erzak gerekiyor.", B.memorialSupplies);
                case "metal":
                    return BoardMaterialFree("metal") >= B.tradeMetal ? null : Loc.F("{0} serbest metal gerekiyor.", B.tradeMetal);
                case "erzak":
                    return Shelter.Supplies >= B.tradeSupplies ? null : Loc.F("{0:0.##} erzak gerekiyor.", B.tradeSupplies);
                case "gonder":
                    return Shelter.Supplies >= B.callSupplies ? null : Loc.F("{0:0.##} erzak gerekiyor.", B.callSupplies);
                case "kabul":
                    // Karar anında karşı saldırı (ve güven kaybı) üretilmesin.
                    return S.Alarm + B.convoyAlarm >= B.liberationAlarmThreshold ? Loc.T("Alarm eşiğe çok yakın.") : null;
                default:
                    return null;
            }
        }

        void ShelterOptionText(ShelterEventInstance e, string option, out string cost, out string risk)
        {
            cost = null;
            risk = null;
            switch (option)
            {
                case "tecrit":
                    cost = Loc.F("{0} vardiya çalışmaz, yer; tecrit yerini tutar.", B.isolationShifts);
                    risk = Loc.T("Tecrit her zaman bulgu vermez.");
                    break;
                case "yolla":
                    cost = Loc.F("Kişi, ustalığı ve kendi teçhizatı gider; {0:0.##} erzak yolluk.", DismissSuppliesPreview);
                    break;
                case "yoksay":
                    // Adalet ilkesi (a): eşiğe vardıran karar kâğıtta söyler; sabotajın ifşa bedeli de risk satırındadır. İkisi de
                    // yalnız public sayaca bakar, kimliğe bakmaz.
                    cost = ReachesExposureThreshold(B.exposureIgnoredTrace)
                        ? Loc.F("İfşa +{0}: eşiğe ulaşır; kapanışta konum çözülür, {1} vardiyan kalır.", B.exposureIgnoredTrace, Math.Max(1, B.exposureDecisionWarnShifts))
                        : Loc.F("İfşa +{0}.", B.exposureIgnoredTrace);
                    risk = e.Kind == "iz" ? Loc.T("Yeni bir iz gelebilir.") : Loc.F("Kişi sızmacıysa sabotaj gelir: ifşa +{0}.", B.exposureSabotage);
                    break;
                case "sok":
                    cost = Loc.T("Kişi listeden çıkar; malzeme gelmez.");
                    break;
                case "sorgula":
                    cost = Loc.F("{0} vardiya yer tutmadan bekletilir, sonra sökülür; ifşa {1}.", B.isolationShifts, B.exposureInterrogation);
                    break;
                case "kisitla":
                    cost = Loc.F("{0} vardiya saha, üretim ve mutfak kapalı; yalnız Boşta açık.",
                        e.Kind == "buyuk_hirsizlik" ? B.eventRestrictLongShifts : B.eventRestrictShifts);
                    break;
                case "affet":
                    cost = Loc.T("Bedel yok.");
                    risk = Loc.T("Yeniden çalabilir.");
                    break;
                case "ikna":
                    cost = Loc.T("Kişi kalır; bir vardiya görev alamaz.");
                    risk = Loc.T("Açlık sürerse ayrılmalar başlar.");
                    break;
                case "birak":
                    cost = Loc.T("Kişi, ustalığı ve kendi teçhizatı gider.");
                    break;
                case "kilitle":
                    cost = Loc.F("Kişi {0} vardiya kısıtlı; güven −1.", B.eventRestrictShifts);
                    break;
                case "yarim":
                    cost = Loc.F("{0} vardiya tüketim yarıya iner; kimse iyileşmez, baskına çıkan {1} can eksik çıkar.", B.halfRationShifts, B.halfRationRaidHp);
                    break;
                case "tam":
                    cost = Loc.T("Değişiklik yok.");
                    risk = Loc.T("Açlık sürerse ayrılmalar başlar.");
                    break;
                case "kaldir":
                    cost = Loc.F("{0:0.##} erzak; kişi {1} canla kalkar.", B.reviveSupplies, B.reviveHp);
                    break;
                case "beklet":
                    cost = Loc.T("Bedel yok.");
                    risk = Loc.T("Yatış uzayabilir.");
                    break;
                case "ayir":
                    cost = Loc.F("{0} vardiya aynı baskına giremezler.", B.feudShifts);
                    break;
                case "karisma":
                    cost = Loc.T("Bedel yok.");
                    risk = Loc.T("Biri yaralanabilir.");
                    break;
                case "an":
                    cost = Loc.F("{0:0.##} erzak; güven +{1}.", B.memorialSupplies, B.memorialTrust);
                    break;
                case "devam":
                    if (e.Kind == "anma")
                    {
                        cost = Loc.T("Bedel yok.");
                        risk = Loc.T("Kadro arkadaşlarından biri bir vardiya görev alamayabilir.");
                    }
                    else cost = Loc.T("Değişiklik yok.");
                    break;
                case "karart":
                    cost = Loc.F("Bu vardiya Üretim ve Mutfak çalışmaz; alarm −{0:0.##}.", B.blackoutAlarm);
                    break;
                case "metal":
                    cost = Loc.F("{0} metal → {1:0.##} erzak.", B.tradeMetal, B.tradeMetalSupplies);
                    break;
                case "erzak":
                    cost = Loc.F("{0:0.##} erzak → {1} devre.", B.tradeSupplies, B.tradeCircuits);
                    break;
                case "reddet":
                    cost = Loc.T("Bedel yok.");
                    break;
                case "gonder":
                    cost = Loc.F("{0:0.##} erzak; güven +{1}.", B.callSupplies, B.callTrust);
                    break;
                case "gonderme":
                    cost = Loc.T("Bedel yok.");
                    risk = Loc.T("Güven düşebilir.");
                    break;
                case "kabul":
                    cost = Loc.F("+{0:0.##} erzak; alarm +{1:0.##}.", B.convoySupplies, B.convoyAlarm);
                    break;
            }
        }

        /// <summary>İfşa şimdi eşiğin altında ve <paramref name="delta"/> onu eşiğe taşır (olaylar ve eşik açıkken). Public sayaçtan türer.</summary>
        bool ReachesExposureThreshold(int delta)
            => ShelterEventsOn && B.exposureThreshold > 0 && delta > 0
               && Shelter.Exposure < B.exposureThreshold && Shelter.Exposure + delta >= B.exposureThreshold;

        /// <summary>Kişiyi sığınakta tutup kısıtlayan ya da tecrit eden seçenek (kit kilidi satırı yalnız bunlarda yazar).</summary>
        static bool KeepsPersonOnBoard(string option) => option == "tecrit" || option == "sorgula" || option == "kisitla" || option == "kilitle";

        /// <summary>
        /// D3: kısıtlı ya da tecritteki kişinin bağlı kiti kit kilidinde "var" sayılır (bedava kit üretilmez). Karşılığında bedel
        /// kâğıtta ayrı satırda yazar: hedef o rolün tek şarjlı kitini bağlı taşıyorsa satır döner, değilse null. Public teçhizat
        /// durumundan türer.
        /// </summary>
        string KitLockLine(string personId)
        {
            var kit = Board.Equipment.FirstOrDefault(x => x.OwnerId == personId && x.Bound && x.Charges > 0 && PlanningBoardData.EquipmentSlot(x.Kind) == "kit");
            if (kit == null || Board.Equipment.Any(x => x != kit && x.Kind == kit.Kind && x.Charges > 0)) return null;
            return Loc.F("Bu sürede {0} kullanılamaz: kendi kiti ondadır.", PlanningBoardData.EquipmentName(kit.Kind));
        }

        // ---------- Komut ----------
        /// <summary>Olay kararının gerekçesi; verilebiliyorsa null. Plan kilidi kararı kapatmaz (aday seçimiyle aynı koşul).</summary>
        public string ShelterEventDecisionCheck(string optionId)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (ShelterEvents == null) return Loc.T("Bu kayıtta sığınak olayı yok.");
            if (S.Raid != null) return Loc.T("Baskın sürüyor.");
            if (Board.Printed) return Loc.T("Fiş basıldı; olay kararı bu vardiya verilemez.");
            var e = PendingShelterEvent;
            if (e == null) return Loc.T("Karar bekleyen olay yok.");
            if (string.IsNullOrEmpty(optionId) || !ShelterEventData.Options(e.Kind).Contains(optionId)) return Loc.T("Bu olayda böyle bir seçenek yok.");
            return ShelterOptionClosed(e, optionId);
        }

        /// <summary>Açık olay fişi kilitler; gerekçe ya da null. Baskın emrini engellemez.</summary>
        public string ShelterEventPrintCheck()
            => PendingShelterEvent != null ? Loc.T("Karar bekleyen olay var: önce olay tutanağını karara bağla.") : null;

        /// <summary>Açık olayı verilen seçenekle karara bağlar. Çekim yapmaz; vadeli sonuçların çekimi sonraki kapanıştadır.</summary>
        public ActResult DecideShelterEvent(string optionId)
        {
            string why = ShelterEventDecisionCheck(optionId);
            if (why != null) return ActResult.Fail(why);
            var e = ShelterEvents.Pending;
            ShelterEvents.Pending = null;
            ApplyShelterEventOption(e, optionId);
            LogShelterEvent(e.Kind, optionId, e.PersonId, e.OtherId, e.Amount);
            OpenFieldDecision();
            return Rec(ActResult.Success(Loc.T("Olay kararı kaydedildi.")), "event_decide", optionId);
        }

        void ApplyShelterEventOption(ShelterEventInstance e, string option)
        {
            var f = e.PersonId == null ? null : Admission.File(e.PersonId);
            var person = f?.Resident;
            var delayed = ShelterEvents.Delayed;
            switch (option)
            {
                case "tecrit":
                    person.Isolated = true;
                    f.IsolatedUntil = S.Day + Math.Max(1, B.isolationShifts);
                    person.Job = "rest";
                    ReleaseResidentEquipment(f.PersonId, false);
                    break;
                case "yolla":
                    SendAway(f, "yollandi", true);
                    break;
                case "yoksay":
                    ChangeExposure(B.exposureIgnoredTrace, e.Kind, e.PersonId);
                    break;
                case "sok":
                    SendAway(f, "sokuldu", false);
                    break;
                case "sorgula":
                    f.IsolatedUntil = S.Day + Math.Max(1, B.isolationShifts);
                    ChangeExposure(B.exposureInterrogation, "sorgula", e.PersonId);
                    break;
                case "kisitla":
                    ApplyEventRestriction(f, e.Kind == "buyuk_hirsizlik" ? B.eventRestrictLongShifts : B.eventRestrictShifts);
                    break;
                case "affet":
                    f.Pardoned = true;
                    delayed.Add(new ShelterDelayed { Kind = "buyuk_hirsizlik", PersonId = f.PersonId, DecidedShift = S.Day });
                    break;
                case "ikna":
                    // Plan kilitliyse bedel sonraki vardiyaya kayar: kilitli planda görev değişmez, kişi bir sonraki vardiya görev alamaz.
                    f.IdleUntil = Math.Max(f.IdleUntil, S.Day + (Board.Locked ? 2 : 1));
                    if (!Board.Locked) person.Job = "rest";
                    break;
                case "birak":
                    SendAway(f, "birakildi", false);
                    Shelter.Deserted++;
                    Shelter.DesertedSinceFull = true;
                    break;
                case "kilitle":
                    ApplyEventRestriction(f, B.eventRestrictShifts);
                    ChangeTrust(null, -1);
                    break;
                case "yarim":
                    Shelter.HalfRationUntil = S.Day + Math.Max(1, B.halfRationShifts);
                    break;
                case "kaldir":
                    Shelter.Supplies -= B.reviveSupplies;
                    person.Down = 0;
                    person.Hp = Math.Min(person.MaxHp, Math.Max(person.Hp, B.reviveHp));
                    break;
                case "beklet":
                    delayed.Add(new ShelterDelayed { Kind = "yatis_uzar", PersonId = e.PersonId, DecidedShift = S.Day });
                    break;
                case "ayir":
                    ShelterEvents.Feuds.Add(new ShelterFeud { A = e.PersonId, B = e.OtherId, Until = S.Day + Math.Max(1, B.feudShifts) });
                    break;
                case "karisma":
                    delayed.Add(new ShelterDelayed { Kind = "kavga_yara", PersonId = e.PersonId, OtherId = e.OtherId, DecidedShift = S.Day });
                    break;
                case "an":
                    Shelter.Supplies -= B.memorialSupplies;
                    ChangeTrust(null, Math.Max(0, B.memorialTrust));
                    break;
                case "devam":
                    if (e.Kind == "anma")
                        delayed.Add(new ShelterDelayed { Kind = "yas", PersonId = e.PersonId, DecidedShift = S.Day, Mates = MemorialMates(e.PersonId) });
                    break;
                case "karart":
                    Shelter.BlackoutShift = S.Day + 1;
                    ChangeAlarm(-Math.Max(0, B.blackoutAlarm), null);
                    break;
                case "metal":
                {
                    var metal = Board.Material("metal");
                    if (metal != null) metal.Quantity -= Math.Min(metal.Quantity, Math.Max(0, B.tradeMetal));
                    SyncBoardMaterialTotal();
                    Shelter.Supplies = Math.Min(B.boardSuppliesCapacity, Shelter.Supplies + Math.Max(0, B.tradeMetalSupplies));
                    break;
                }
                case "erzak":
                    Shelter.Supplies -= B.tradeSupplies;
                    AddBoardMaterial("devre", B.tradeCircuits);
                    break;
                case "gonder":
                    Shelter.Supplies -= B.callSupplies;
                    ChangeTrust(null, Math.Max(0, B.callTrust));
                    break;
                case "gonderme":
                    delayed.Add(new ShelterDelayed { Kind = "cagri_guven", DecidedShift = S.Day });
                    break;
                case "kabul":
                    Shelter.Supplies = Math.Min(B.boardSuppliesCapacity, Shelter.Supplies + Math.Max(0, B.convoySupplies));
                    ChangeAlarm(Math.Max(0, B.convoyAlarm), null);
                    break;
                // "tam", "reddet": etkisiz.
            }
        }

        /// <summary>
        /// Olay kısıtı: yalnız Eğitim ve Boşta açık; gözetmen yok, birden çok kişi kısıtlı olabilir, süre dolunca kendiliğinden
        /// kalkar. Bağlı olmayan teçhizat depoya döner, bağlı olan kişide kalır.
        /// </summary>
        void ApplyEventRestriction(ResidentAdmissionFile f, int shifts)
        {
            f.Resident.Restricted = true;
            f.RestrictedUntil = S.Day + Math.Max(1, shifts);
            if (f.Resident.Job != "train") f.Resident.Job = "rest";
            ReleaseResidentEquipment(f.PersonId, false);
        }

        /// <summary>Ölenin o baskındaki, hâlâ sığınakta ve sağ olan kadro arkadaşları (kadro sırasıyla).</summary>
        List<string> MemorialMates(string deadId)
        {
            for (int i = S.RaidLog.Count - 1; i >= 0; i--)
            {
                var squad = S.RaidLog[i].LiberationReward?.Squad;
                if (squad == null || !squad.Any(x => x.PersonId == deadId && x.Dead)) continue;
                return squad.Where(x => x.PersonId != deadId && !x.Dead && InShelter(x.PersonId)).Select(x => x.PersonId).Distinct().ToList();
            }
            return new List<string>();
        }

        /// <summary>
        /// Hedefi giden olay kapanır: kişisi karar verilmeden ölür ya da sığınaktan çıkarsa olay kendiliğinden kapanır, aralığı
        /// ve tekrar sınırını tüketmez; fiş kilidi kalkar. Yapılmış çekim geri alınmaz.
        /// </summary>
        void CloseOrphanShelterEvent()
        {
            var e = PendingShelterEvent;
            if (e == null || !ShelterEventData.NeedsLivingTarget(e.Kind)) return;
            if (InShelter(e.PersonId) && (e.Kind != "kavga" || InShelter(e.OtherId))) return;
            ShelterEvents.Pending = null;
            var c = EventCount(e.Kind);
            c.Count = Math.Max(0, c.Count - 1);
            c.LastShift = e.PrevLastShift;
            if (ShelterEventData.IsOrdinary(e.Kind)) ShelterEvents.NextEventShift = e.PrevNextEventShift;
            LogShelterEvent("kapandi", e.Kind, e.PersonId, e.OtherId, e.Amount);
        }

        /// <summary>Kişi sığınaktan çıkarken olay durumları temizlenir (tecrit yeri boşalır, kavga kaydı düşer).</summary>
        void ClearShelterPersonState(ResidentAdmissionFile f)
        {
            f.Resident.Isolated = false;
            f.IsolatedUntil = -1;
            f.IdleUntil = -1;
            f.Resident.Restricted = false;
            f.RestrictedUntil = -1;
            ShelterEvents?.Feuds.RemoveAll(x => x.A == f.PersonId || x.B == f.PersonId);
        }
    }
}
