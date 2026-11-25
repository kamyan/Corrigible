using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kabul istasyonunun anlık görüntüsünü Core'un salt okunur durumundan kurar. Saf: rastgele sayı çekmez, komut yazmaz, durumu
    /// değiştirmez; gizli kimliği hiç okumaz. Bütün görünür metin burada, o anki dilde (<see cref="Loc"/>); sayılar <see cref="DeskFormat"/>
    /// ile. Renkler tema anahtarından (<see cref="SiginakTheme"/>) "#RRGGBB".
    /// <para>
    /// Kapıda 2–3 kişilik aday grubu durur (<see cref="AdmissionSnapshot.Group"/>). Tezgâh yalnız grup beklerken iner. KABUL damgası
    /// bir karta (<c>ChooseCandidate</c>), GERİ ÇEVİR grup fişine (<c>PassCandidates</c>) basılır; etkinlik ve nedenler Core'un
    /// <see cref="Simulation.CandidateChoiceCheck"/> ve <see cref="Simulation.CandidatePassCheck"/> denetimlerinden aynen. Grup yalnız
    /// açık durumdan okunur (kişi, kademe, şüphe satırı, özellikler, bekleme); kimlik, iz ve geçmiş okunmaz.
    /// </para>
    /// <para>
    /// Eski tek adaylı kabul (inceleme araçları, belge, talimat şeridi, bulgu fişleri, dış kayıt talebi, gözetmen) aşama 7'de Core'dan ve
    /// sunumdan söküldü.
    /// </para>
    /// </summary>
    public static class AdmissionSnapshotBuilder
    {
        /// <summary>Kapıda bekleyen aday grubu var mı (kartlı oyun, harekât bitmediyse).</summary>
        public static bool GroupWaiting(Simulation sim) =>
            sim?.Admission != null && sim.Board != null && sim.Admission.Group.Count > 0 && sim.S.Ending == null;

        /// <summary>Grubun sunum kimliği ("group:{geliş vardiyası}:{ilk aday}"): vuruş, yerleşim ve işaretli kişi buna bağlıdır; grup yoksa null.</summary>
        public static string GroupKey(Simulation sim)
        {
            var adm = sim?.Admission;
            if (adm == null || adm.Group.Count == 0) return null;
            return "group:" + adm.GroupArrivalShift.ToString(CultureInfo.InvariantCulture) + ":" + adm.Group[0].Resident.Id;
        }

        /// <summary>Grup bu kapanışta gider mi (Core'un süre aşımıyla aynı kural: <c>Day + 1 &gt;= GroupWaitingUntil</c>).</summary>
        public static bool GroupLastShift(Simulation sim) => GroupWaiting(sim) && sim.S.Day + 1 >= sim.Admission.GroupWaitingUntil;

        public static AdmissionSnapshot Build(Simulation sim, AdmissionUi ui)
        {
            ui = ui ?? new AdmissionUi();
            var s = new AdmissionSnapshot { Words = new AdmissionWords() };
            FillWords(s.Words);
            var adm = sim?.Admission;
            s.Active = adm != null && sim.Board != null;
            s.LockReason = s.Active ? CommandLock(sim) : null;
            s.Locked = s.LockReason != null;
            s.CounterHoverTitle = Loc.T("KABUL TEZGÂHI");

            s.Group = s.Active && sim.S.Ending == null ? GroupOf(sim, adm, ui) : null;
            s.Counter = s.Group != null ? CounterState.Down : CounterState.Folded;
            s.CounterHoverLine = s.Group != null ? Loc.F("{0} aday bekliyor", s.Group.Cards.Count) : Loc.T("aday yok");

            FillGroupStamps(s, sim);
            return s;
        }

        // ---------------------------------------------------------------- kilit

        /// <summary>
        /// Kapı komutlarının kilidi, yalnız Core denetiminden: kapıda grup varken <see cref="Simulation.CandidatePassCheck"/> (oyun bitti /
        /// baskın / fiş basıldı); grup yokken kapıda komut yoktur, kilit null.
        /// </summary>
        static string CommandLock(Simulation sim) => sim.Admission.Group.Count > 0 ? sim.CandidatePassCheck() : null;

        // ---------------------------------------------------------------- aday grubu

        /// <summary>
        /// Aday grubunun görüntüsü (saf okuma; yalnız açık durum): kartlar (ad, ustalık, beden, kademe, şüphe satırı, özellikler; seçim
        /// denetimi Core'dan), grup fişi (bekleme, acil, ipucu), dolu sığınakta yollanabilir kişiler ve yolluk ön izlemesi.
        /// </summary>
        static CandidateGroupData GroupOf(Simulation sim, CommunityAdmissionState adm, AdmissionUi ui)
        {
            if (adm.Group.Count == 0) return null;
            var b = sim.B;
            int alive = sim.Board.People.Count(p => p.Alive), capacity = b.boardResidentCapacity;
            var g = new CandidateGroupData { Key = GroupKey(sim), Emergency = adm.GroupEmergency, Full = alive >= capacity };
            g.WaitLeft = Math.Max(0, adm.GroupWaitingUntil - sim.S.Day);
            g.LastShift = sim.S.Day + 1 >= adm.GroupWaitingUntil;
            string replace = g.Full ? ui.Replace(g.Key) : null;
            // İşaretli kişi artık panoda değilse (öldü, gitti) seçim düşer
            if (replace != null && !sim.Board.People.Any(p => p.Alive && p.Id == replace)) replace = null;
            g.ReplaceId = replace;

            foreach (var c in adm.Group)
            {
                var r = c.Resident;
                string why = sim.CandidateChoiceCheck(r.Id, replace);
                var card = new CandidateCardData
                {
                    Id = r.Id, Name = DeskText.Upper(r.Name),
                    PortraitKey = PlanningBoardSnapshotBuilder.PortraitKeyOf(r.Name), PortraitFlip = NumberedName(r.Name),
                    Arrived = ShiftText(c.ArrivalShift),
                    Mastery = Loc.F("{0} · {1}", PlanningBoardSnapshotBuilder.TopicLabel(r.TrainingSubject),
                        PlanningBoardSnapshotBuilder.TierWord(sim.BoardSkillTier(r, r.TrainingSubject))),
                    Stats = Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", r.Hp, r.MaxHp, r.Move, r.Power),
                    Tier = c.Tier, TierName = DeskText.Upper(CandidateData.TierName(c.Tier)),
                    TierColor = ThemeHex("admission.candidates.tier" + c.Tier.ToString(CultureInfo.InvariantCulture)),
                    Suspicion = CandidateData.SuspicionLine(c.SuspicionKey, c.SuspicionArg),
                    CanChoose = why == null, ChooseReason = why,
                    HoverLine = why ?? Loc.T("KABUL damgasını bu karta bas."),
                };
                foreach (string perk in c.Perks)
                {
                    bool cost = CandidateData.IsCost(perk);
                    card.Perks.Add(new PerkRowData
                    {
                        Id = perk, Cost = cost, Tag = cost ? Loc.T("BEDEL") : Loc.T("GETİRİ"),
                        Name = CandidateData.PerkName(perk, b), Effect = CandidateData.PerkEffect(perk, b, r.TrainingSubject),
                    });
                }
                g.Cards.Add(card);
            }

            string rations = Amount(sim.DismissSuppliesPreview);
            g.SlipHeader = Loc.T("KYGA · KAPI FİŞİ");
            g.SlipShift = ShiftText(adm.GroupArrivalShift);
            g.SlipTitle = Loc.F("{0} ADAY", adm.Group.Count);
            g.WaitLine = g.LastShift ? Loc.T("SON VARDİYA · karar yoksa giderler") : Loc.F("bekleme: {0} vardiya", g.WaitLeft);
            g.EmergencyLine = g.Emergency ? Loc.T("ACİL · kadro eksik: her aday kendi kitiyle geldi") : null;
            g.HintLine = Loc.T("En çok biri alınır. KABUL: bir karta. GERİ ÇEVİR: bu fişe.");
            g.RoomTitle = g.Full ? Loc.F("YER YOK · {0}/{1}", alive, capacity) : Loc.F("YER VAR · {0}/{1}", alive, capacity);
            g.RoomHint = g.Full ? Loc.F("Aday için birini yolla: satıra tıkla. Yolluk: {0} erzak.", rations) : null;
            if (g.Full)
                foreach (var p in sim.Board.People.Where(p => p.Alive))
                {
                    string no = sim.CandidateChoiceCheck(adm.Group[0].Resident.Id, p.Id);
                    // Yer kısa yazılır (eğitimde konu eklenmez): satır 57 mm'lik sütuna sığmalı
                    string place = BoardDropRules.PlaceOf(sim.Board, p, BoardDropRules.WatchOpen(sim));
                    string job = place == "train" ? PlanningBoardData.JobName("train") : PlanningBoardSnapshotBuilder.PlaceText(p, place);
                    g.Replace.Add(new ReplaceRowData
                    {
                        Id = p.Id, Name = p.Name, Job = job, Label = Loc.F("{0} · {1}", p.Name, job),
                        Selected = p.Id == replace, Enabled = no == null, Reason = no,
                        BoundGear = sim.Board.Equipment.Any(e => e.OwnerId == p.Id && e.Bound),
                    });
                }
            var sel = g.Replace.Find(x => x.Selected);
            g.ReplaceLine = sel == null ? null : sel.BoundGear
                ? Loc.F("{0} yollanacak · yolluk {1} erzak · bağlı teçhizatı onunla gider", sel.Name, rations)
                : Loc.F("{0} yollanacak · yolluk {1} erzak", sel.Name, rations);
            string pass = sim.CandidatePassCheck();
            g.CanPass = pass == null;
            g.PassReason = pass;
            g.SlipHoverTitle = Loc.T("KAPI FİŞİ");
            g.SlipHoverLine = pass ?? Loc.T("GERİ ÇEVİR damgasını bu fişe bas.");
            g.RowHoverLine = Loc.T("Tıkla: yollanacak kişi olarak işaretle.");
            return g;
        }

        /// <summary>Seçilen karta KABUL izini yazar (komuttan ÖNCEKİ görüntünün üstüne; komuttan sonra grup Core'dan düşer).</summary>
        public static void StampCard(CandidateGroupData g, string candidateId, Balance b)
        {
            var card = g?.Cards.Find(c => c.Id == candidateId);
            if (card == null) return;
            card.StampChoiceId = AdmissionIds.ChoiceOf(StampChoice.Admit);
            card.StampLines = ImpressionLines(StampChoice.Admit, b);
            card.StampColor = StampColorHex(StampChoice.Admit);
        }

        /// <summary>Grup fişine GERİ ÇEVİR izini yazar (komuttan önceki görüntünün üstüne).</summary>
        public static void StampPass(CandidateGroupData g)
        {
            if (g == null) return;
            g.PassStampLines = new[] { Loc.T("GERİ ÇEVRİLDİ") };
            g.PassStampColor = StampColorHex(StampChoice.Reject);
        }

        // ---------------------------------------------------------------- sözcükler

        static void FillWords(AdmissionWords w)
        {
            w.Drawer = Loc.T("EVRAK");
            w.Under = Loc.T("KABUL");
            w.Desk = Loc.T("MASA");
        }

        // ---------------------------------------------------------------- damgalar

        /// <summary>Raf plakası (iki satır; rafın <c>TXT_S2</c> ve <c>TXT_S3</c> yazılarıyla aynı metin). KISITLI ve YARDIM'ın plakası yok.</summary>
        public static string StampPlate(StampChoice choice)
        {
            switch (choice)
            {
                case StampChoice.Admit: return Loc.T("KABUL ·\nTAM ERİŞİM");
                case StampChoice.Reject: return Loc.T("GERİ\nÇEVİR");
                default: return string.Empty;
            }
        }

        /// <summary>Damganın sapındaki kısa ad (TXT_Cap). EN'de tasarımın ADMIT sözcüğü (tezgâh şablonu ve dosyadaki KABUL aynen kalır).</summary>
        public static string StampCap(StampChoice choice)
        {
            switch (choice)
            {
                case StampChoice.Admit: return DeskLoc.Instance.Ctx("stamp", "KABUL");
                case StampChoice.Reject: return Loc.T("GERİ");
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Dört damga (<see cref="StampChoice"/> sırası). KABUL: gruptaki en az bir kart seçilebiliyorsa açık (Core'un
        /// <see cref="Simulation.CandidateChoiceCheck"/>'i; dolu sığınakta işaretli kişiyle); neden ilk kartın Core iletisi, kısa neden
        /// yalnız "dolu ve kimse işaretlenmedi" durumunda "YER YOK n/n" (canlı sayısı ve kapasite açık değerlerden). GERİ ÇEVİR:
        /// <see cref="Simulation.CandidatePassCheck"/>. KISITLI ve YARDIM'ın damgası yok (aşama 7'de söküldü): dizideki yerleri metinsiz, kapalı
        /// boş veridir (dizin <see cref="StampChoice"/> sayısıdır).
        /// </summary>
        static void FillGroupStamps(AdmissionSnapshot s, Simulation sim)
        {
            var g = s.Group;
            string none = Loc.T("Bu kayıtta topluluk kabulü yok.");
            for (int i = 0; i < s.Stamps.Length; i++) s.Stamps[i] = new StampData { Plate = "", Cap = "" };
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                string why, shortWhy = null;
                switch (choice)
                {
                    case StampChoice.Admit:
                        if (!s.Active) why = none;
                        else if (g == null) why = sim.CandidatePassCheck();
                        else if (g.Cards.Any(c => c.CanChoose)) why = null;
                        else
                        {
                            why = g.Cards.Count > 0 ? g.Cards[0].ChooseReason : sim.CandidatePassCheck();
                            if (g.Full && g.ReplaceId == null && s.LockReason == null)
                                shortWhy = Loc.F("YER YOK {0}/{1}", sim.Board.People.Count(p => p.Alive), sim.B.boardResidentCapacity);
                        }
                        break;
                    default:
                        why = !s.Active ? none : sim.CandidatePassCheck();
                        break;
                }
                s.Stamps[(int)choice] = new StampData
                {
                    Plate = StampPlate(choice), Cap = StampCap(choice), Reason = why, ShortReason = shortWhy, Enabled = s.Active && why == null,
                    BandColor = StampColorHex(choice),
                };
            }
        }

        /// <summary>Damga izinin satırları (KK <c>damga_*</c>): sayılar denge değerlerinden.</summary>
        public static string[] ImpressionLines(StampChoice choice, Balance b)
        {
            switch (choice)
            {
                // "KABUL" tek başına ADMISSION (tezgâh şablonu, dosya etiketi); izin iki satırı "KABUL · TAM ERİŞİM" anahtarından (EN ADMIT · FULL ACCESS)
                case StampChoice.Admit: return Loc.T("KABUL · TAM ERİŞİM").Split(new[] { " · " }, StringSplitOptions.RemoveEmptyEntries);
                case StampChoice.Reject: return new[] { Loc.T("REDDET") };
                default: return new string[0];
            }
        }

        /// <summary>İzin ve bandın tema rengi (<c>admission.stampMarks.{seçim}.color</c>) "#RRGGBB"; tema yoksa null.</summary>
        public static string StampColorHex(StampChoice choice) => ThemeHex("admission.stampMarks." + AdmissionIds.ChoiceOf(choice) + ".color");

        static string ThemeHex(string key)
        {
            var theme = SiginakTheme.Active;
            return theme != null && theme.TryGet(key, out var color) ? "#" + ColorUtility.ToHtmlStringRGB(color) : null;
        }

        /// <summary>Tam sayıysa ondalıksız (2), değilse bir ondalık (2,5 / 2.5).</summary>
        static string Amount(double v) => Math.Abs(v - Math.Round(v)) < 1e-9 ? DeskFormat.Int((int)Math.Round(v)) : DeskFormat.Number(v, 1, Lang);

        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;

        // ---------------------------------------------------------------- portre

        /// <summary>
        /// Sayılı ad ("Frank 9"): portre anahtarı sayıyı atar (Frank'in yüzü); sunum yüzü belirlenimci aynalar (D18-B). Yalnız ada
        /// bakar, gizli kimliğe asla.
        /// </summary>
        public static bool NumberedName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.Trim();
            int space = n.LastIndexOf(' ');
            return space > 0 && space < n.Length - 1 && n.Substring(space + 1).All(char.IsDigit);
        }

        static string ShiftText(int day) => Loc.F("V.{0:000}", day + 1);
    }
}
