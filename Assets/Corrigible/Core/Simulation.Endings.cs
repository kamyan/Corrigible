using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Sığınağın sonları (kartlı yol): ifşa eşiği ve "konum çözüldü" vardiyası (dusus), açlıkla boşalan sığınak (terk), oyuncunun
    /// çektiği son şalter (salter) ve zaferin tonu. Kapanış adımı ve okuyucular çekim yapmaz. Gizli kimlikten türeyen bilgi
    /// (<see cref="VictoryTone"/>, <see cref="EndReport"/>) yalnız son varken döner.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>"Konum çözüldü" uyarısı: ifşa eşikte ya da son vardiya işliyor. Olaylar kapalıyken ya da eşik kapalıyken false.</summary>
        public bool LocationCompromised => ShelterEventsOn && B.exposureThreshold > 0
            && (Shelter.FallPending || Shelter.Exposure >= B.exposureThreshold);

        /// <summary>
        /// "Konum çözüldü" uyarısının kalan vardiyası: uyarı sürerken bu vardiya dahil (1: bu vardiya son), ifşa eşikte ama uyarı
        /// henüz kurulmamışken bu kapanışın kuracağı uyarının süresi (kararla varılan eşikte <c>exposureDecisionWarnShifts</c>,
        /// sabotajla 1). <see cref="LocationCompromised"/> değilse 0. Public durumdan türer, çekim yapmaz.
        /// </summary>
        public int FallShiftsLeft => LocationCompromised ? Shelter.FallGrace + 1 : 0;

        /// <summary>İfşanın alarm ara bedeli: ifşa <c>exposureAlarmAt</c> ve üstündeyken vardiya alarmına eklenen değer; yoksa 0.</summary>
        public double ExposureAlarmBonus => ShelterEventsOn && B.exposureAlarmAt > 0 && Shelter.Exposure >= B.exposureAlarmAt
            ? Math.Max(0, B.exposureAlarmBonus) : 0;

        /// <summary>
        /// Terk sayacı işliyor: terk açık, sığınak tam kadrodan beri ayrılma yaşadı, canlı sayısı acil eşiğinin altında ve sayaç
        /// ya sürüyor ya da bu kapanışta işleyecek (vardiya kapıda acil grupla açıldı). Tahliye nedeni ("kadro") ve fiş satırı
        /// bunu okur. Public durumdan türer, çekim yapmaz.
        /// </summary>
        public bool UnderstaffedCounting => ShelterEventsOn && B.abandonUnderstaffedShifts > 0 && Shelter.DesertedSinceFull
            && BoardAliveCount < B.candidateEmergencyBelow
            && (Shelter.UnderstaffedStreak > 0 || Shelter.EmergencyDoorShift == S.Day + 1);

        /// <summary>
        /// Durum değişmezse bu kapanıştan sonraki terk sayacı (fiş "SIĞINAK EKSİK n/3" bunu yazar; n eşiğe ulaşırsa terk bu
        /// kapanışta gelir). Sayaç işlemiyorsa 0.
        /// </summary>
        public int UnderstaffedAtClose => !UnderstaffedCounting ? 0
            : Shelter.UnderstaffedStreak + (Shelter.EmergencyDoorShift == S.Day + 1 ? 1 : 0);

        // ---------- Kapanış: sayaçlar ve sonlar (çekim yok) ----------
        /// <summary>
        /// Step'in altıncı adımı (kapıdan sonra, olay açılışından önce): kapanışın sonları. Sıra: eksik kadro sayacı ve ayrılma
        /// dönemi; <c>topluluk</c>; önceki kapanışta kurulmuş "konum çözüldü" sürüyorsa (ifşa vardiya boyunca eşiğin altına inmedi;
        /// inseydi uyarı o anda kalkardı) ve uyarı süresi bittiyse (<see cref="ShelterState.FallGrace"/>; değilse bir azalır)
        /// <c>dusus</c>; <c>terk</c>. Sonlar olay açılışından önce sınanır: son kapanışta olay
        /// kâğıdı açılmaz ve bu kapanışta açılan bir sabotaj düşüşün nedeni olamaz (K4: sabotajla varılan eşik yalnız uyarır). Olay
        /// açılışı canlı sayısını değiştirmediği için öncelik ve terk sonucu açılıştan bağımsızdır. Son gelirse alarm adımı işlemez
        /// (dagilma en sonda). Çekim yapmaz.
        /// </summary>
        void AdvanceShelterEndings()
        {
            if (!ShelterEventsOn)
            {
                if (BoardAliveCount == 0) End("topluluk");
                return;
            }
            int alive = BoardAliveCount;
            // Sayım yalnız tam kadrodan beri ayrılma yaşanmış sığınakta ve vardiya kapıda acil grupla açıldıysa artar; olağan
            // grup kapıdayken tutulur. Eşikte ya da üstünde kapanan vardiya sayacı ve ayrılma dönemini sıfırlar.
            if (alive >= B.candidateEmergencyBelow)
            {
                Shelter.UnderstaffedStreak = 0;
                Shelter.DesertedSinceFull = false;
            }
            else if (Shelter.DesertedSinceFull && Shelter.EmergencyDoorShift == S.Day) Shelter.UnderstaffedStreak++;

            if (alive == 0) End("topluluk");
            if (S.Ending != null) return;

            if (Shelter.FallPending)
            {
                if (B.exposureThreshold > 0 && Shelter.Exposure >= B.exposureThreshold)
                {
                    // Kararla varılan eşik: uyarı süresi bitmeden kapanış düşürmez (ikinci tur kararı).
                    if (Shelter.FallGrace > 0) Shelter.FallGrace--;
                    else { End("dusus"); return; }
                }
                else
                {
                    // Kurulumla yazılmış durum için güvenlik: sayaç eşiğin altına inince uyarı zaten ChangeExposure'da kalkar.
                    Shelter.FallPending = false;
                    Shelter.FallGrace = 0;
                    LogShelterEvent("konum_kalkti");
                }
            }
            if (B.abandonUnderstaffedShifts > 0 && Shelter.DesertedSinceFull && Shelter.UnderstaffedStreak >= B.abandonUnderstaffedShifts)
                End("terk");
        }

        /// <summary>
        /// Step'in yedinci adımı (olay açılışından sonra, alarmdan önce): ifşa eşikteyse "konum çözüldü" uyarısı kurulur (kararla
        /// varıldıysa o vardiyanın, bu kapanışta açılan sabotajla varıldıysa bu kapanışın uyarısı); kapının acil grubu ertesi
        /// vardiya için yazılır. Son geldiyse bir şey yapmaz. Çekim yapmaz.
        /// </summary>
        void AdvanceShelterWarnings()
        {
            if (!ShelterEventsOn || S.Ending != null) return;
            if (!Shelter.FallPending && B.exposureThreshold > 0 && Shelter.Exposure >= B.exposureThreshold)
            {
                Shelter.FallPending = true;
                // Miktar: uyarının süresi (vardiya; teleks okur).
                LogShelterEvent("konum", amount: Shelter.FallGrace + 1);
            }
            Shelter.EmergencyDoorShift = Admission.Group.Count > 0 && Admission.GroupEmergency ? S.Day + 1 : -1;
        }

        // ---------- Komut: son şalter ----------
        /// <summary>
        /// Son şalterin gerekçesi; çekilebiliyorsa null. Kilitli plan, basılmış fiş ve açık olay engel değildir. Olaylardan
        /// bağımsızdır (kartlı yolda geçerli); rehber kapatması sunumdadır. Çekim yapmaz.
        /// </summary>
        public string LastSwitchCheck()
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Board == null || Admission == null) return Loc.T("Bu kayıtta son şalter yok.");
            if (S.Raid != null) return Loc.T("Baskın sürüyor.");
            if (BoardAliveCount == 0) return Loc.T("Oyun bitti.");
            if (S.Day + 1 < B.lastSwitchMinShift) return Loc.F("Son şalter {0}. vardiyadan önce çekilemez.", B.lastSwitchMinShift);
            return null;
        }

        /// <summary>
        /// Şalter şimdi çekilse tahliye sayılmasının nedenleri; boşsa düzenli çekilme. "konum": <see cref="LocationCompromised"/>;
        /// "kadro": <see cref="UnderstaffedCounting"/> (ayrılmadan sonraki ilk eksik vardiya da sayılır); "aclik":
        /// <see cref="DesertionsOngoing"/> (aç sığınaktan ayrılmalar sürüyor); "guven": güven <c>evacuationTrustBelow</c>'un
        /// altında. Yalnız public durumdan türer, çekim yapmaz (onay kâğıdı da okur).
        /// </summary>
        public List<string> LastSwitchReasons()
        {
            var list = new List<string>();
            if (Board == null || Shelter == null) return list;
            if (LocationCompromised) list.Add("konum");
            if (UnderstaffedCounting) list.Add("kadro");
            if (DesertionsOngoing) list.Add("aclik");
            if (S.Trust < B.evacuationTrustBelow) list.Add("guven");
            return list;
        }

        /// <summary>Son şalteri çeker: harekât <c>salter</c> ile biter; çeşit (çekilme ya da tahliye) o anki durumdan yazılır.</summary>
        public ActResult PullLastSwitch()
        {
            string why = LastSwitchCheck();
            if (why != null) return ActResult.Fail(why);
            var reasons = LastSwitchReasons();
            Shelter.SwitchKind = reasons.Count > 0 ? "tahliye" : "cekilme";
            Shelter.SwitchReasons = reasons;
            Shelter.SwitchShift = S.Day;
            End("salter");
            return Rec(ActResult.Success(reasons.Count > 0
                ? Loc.T("Son şalter çekildi: tahliye emri damgalandı.")
                : Loc.T("Son şalter çekildi: çekilme emri damgalandı.")), "last_switch");
        }

        // ---------- Son okuyucuları (yalnız son varken) ----------
        /// <summary>Zaferin tonu ("temiz", "bedelli", "golgeli"); yalnız kartlı yolda <c>silme</c> sonunda, yoksa null.</summary>
        public string VictoryTone => S.Ending == "silme" && Board != null && Admission != null ? Tone(VictoryCriteria()) : null;

        /// <summary>Son raporu verisi (kimlik, ad, sayı; metin sunumda kurulur). Son yokken ya da eski oda yolunda null.</summary>
        public EndReport EndReport => S.Ending == null || Board == null || Admission == null ? null : BuildEndReport();

        static string Tone(List<EndCriterion> rows)
            => !rows[0].Met ? "golgeli" : rows.All(r => r.Met) ? "temiz" : "bedelli";

        /// <summary>Temiz zaferin ölüm sınırı: taban <c>victoryCleanMaxDeaths</c>, her <c>victoryCleanDeathShifts</c> vardiyada (VARDİYA sayacı) bir artar.</summary>
        int VictoryDeathLimit => B.victoryCleanMaxDeaths + (B.victoryCleanDeathShifts > 0 ? (S.Day + 1) / B.victoryCleanDeathShifts : 0);

        /// <summary>Ortaya çıkmamış sızmacı dosyası (izli ya da izsiz; nerede olursa).</summary>
        static bool HiddenInfiltrator(ResidentAdmissionFile f) => !f.Founder && f.Identity != null && f.Identity.Robot && !f.Revealed;

        /// <summary>
        /// Ton ölçütleri sırayla; her biri "temiz zafer" için tutması gerekeni yazar: golge (izi görmezden gelinmiş, ortaya çıkmamış
        /// sızmacı içeride değil; araştırılmış ama bulgu çıkmamış iz gölgelemez), olum (sınır harekât uzunluğuyla büyür), aclik,
        /// ifsa, guven. İlki tutmazsa ton gölgeli, hepsi tutarsa temiz, değilse bedelli.
        /// </summary>
        List<EndCriterion> VictoryCriteria()
        {
            int minIgnored = Math.Max(1, B.victoryShadowIgnoredTraces);
            int shadows = Admission.Files.Count(f => HiddenInfiltrator(f) && !f.Departed && f.Resident.Alive
                && IgnoredTraces(f.PersonId) >= minIgnored && Board.People.Contains(f.Resident));
            int deaths = Board.People.Count(p => !p.Alive);
            int deathLimit = VictoryDeathLimit;
            return new List<EndCriterion>
            {
                new EndCriterion { Id = "golge", Value = shadows, Limit = 0, Met = shadows == 0 },
                new EndCriterion { Id = "olum", Value = deaths, Limit = deathLimit, Met = deaths <= deathLimit },
                new EndCriterion { Id = "aclik", Value = Shelter.Deserted, Limit = 0, Met = Shelter.Deserted == 0 },
                new EndCriterion { Id = "ifsa", Value = Shelter.Exposure, Limit = B.victoryCleanMaxExposure, Met = Shelter.Exposure <= B.victoryCleanMaxExposure },
                new EndCriterion { Id = "guven", Value = Shelter.MinTrust, Limit = B.victoryCleanMinTrust, Met = Shelter.MinTrust >= B.victoryCleanMinTrust },
            };
        }

        int IgnoredTraces(string personId) => ShelterEvents == null ? 0
            : ShelterEvents.Log.Count(x => x.PersonId == personId && x.Option == "yoksay" && (x.Kind == "iz" || x.Kind == "ikinci_iz"));

        static string FileWhere(ResidentAdmissionFile f)
            => !f.Resident.Alive ? "oldu" : f.Departed ? f.DepartReason ?? "ayrildi" : "icerde";

        EndPerson PersonRow(ResidentAdmissionFile f, ResidentState p)
        {
            p = p ?? f?.Resident;
            return new EndPerson
            {
                Id = p?.Id ?? f?.PersonId, Name = p?.Name, Where = f != null ? FileWhere(f) : p != null && !p.Alive ? "oldu" : "icerde",
                Shift = f != null ? f.DepartShift : -1, AcceptedShift = f != null ? f.AcceptedShift : -1,
                Clues = f?.ClueCount ?? 0, IgnoredTraces = IgnoredTraces(p?.Id ?? f?.PersonId),
                Founder = f != null && f.Founder, Stretcher = p != null && p.Alive && p.Down > 0,
                Infiltrator = f != null && HiddenInfiltrator(f),
            };
        }

        EndReport BuildEndReport()
        {
            var files = Admission.Files;
            var regions = S.Liberation.Regions;
            var r = new EndReport
            {
                Ending = S.Ending, Shift = S.Day,
                Exposure = Shelter.Exposure, Deserted = Shelter.Deserted, MinTrust = Shelter.MinTrust,
                Deaths = Board.People.Count(p => !p.Alive),
                RegionsFree = regions.Count(x => x.Completed), SectorsFree = regions.Sum(x => x.CapturedCount),
                Admitted = files.Count(f => !f.Founder),
                Sent = files.Count(f => f.DepartReason == "yollandi"),
                SentHumans = files.Count(f => f.DepartReason == "yollandi" && !(f.Identity?.Robot ?? false)),
                Removed = files.Count(f => f.DepartReason == "sokuldu"),
                Passed = Admission.Passed.Count,
                PassedInfiltrators = Admission.Passed.Count(x => x.Robot),
            };
            if (S.Ending == "silme")
            {
                r.Criteria = VictoryCriteria();
                r.Tone = Tone(r.Criteria);
            }
            if (S.Ending == "salter")
            {
                r.SwitchKind = Shelter.SwitchKind;
                r.SwitchReasons = new List<string>(Shelter.SwitchReasons ?? new List<string>());
            }
            foreach (var f in files.Where(HiddenInfiltrator)) r.Hidden.Add(PersonRow(f, null));
            foreach (var f in files.Where(f => f.DepartReason == "yollandi" && !(f.Identity?.Robot ?? false))) r.SentHumanList.Add(PersonRow(f, null));
            foreach (var p in Board.People)
            {
                var row = PersonRow(Admission.File(p.Id), p);
                if (p.Alive) r.Survivors.Add(row); else r.Dead.Add(row);
            }
            foreach (var f in files.Where(f => f.DepartReason == "aclik" || f.DepartReason == "birakildi")) r.Deserters.Add(PersonRow(f, null));
            if (ShelterEvents != null)
                foreach (var x in ShelterEvents.Log.Where(x => x.Kind == "ifsa"))
                    r.ExposureSteps.Add(new EndExposureStep { Shift = x.Shift, Cause = x.Option, PersonId = x.PersonId, Name = EventPersonName(x.PersonId), Delta = (int)x.Amount });
            return r;
        }
    }
}
