using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kabul istasyonunun anlık görüntüsünü Core'un salt okunur durumundan kurar (Revizyon 3 · Aşama 6, PLAN §1.2, §1.7–§1.10): belge,
    /// şerit + arşiv, araç ve damga kilitleri (Core denetimleri aynen), fişler (<see cref="Simulation.AdmissionFindingCode"/> → damga
    /// sözcüğü; gövde Core verisinden), talep formu, gözetmen adı (<see cref="Simulation.AdmissionWatcher"/>), uyarılar. Saf: rastgele sayı
    /// çekmez, komut yazmaz, durumu değiştirmez; gizli kimliği (robotluk, kusur, model) hiç okumaz. Bütün görünür metin burada, o anki
    /// dilde (<see cref="Loc"/>); sayılar <see cref="DeskFormat"/> ile. Renkler tema anahtarından (<see cref="SiginakTheme"/>) "#RRGGBB".
    /// <para>
    /// Kurallar: aday yoksa belge, fiş ve talep değerleri boştur; tezgâh aday varken ya da görülmemiş talimat varken iner
    /// (<see cref="AdmissionUi.SeenOrdersVersion"/> 0 = bilinmiyor, "yeni" sayılmaz). Fiş sırası alttan üste: İNCELEME FİŞİ (varsa), sonra
    /// incelemelerin yapılış sırası; dış kayıt sonucu BEKLENİYOR fişinin yerini alır — canlı sunum durumunda (<see cref="AdmissionUi.SeenOrdersVersion"/>
    /// &gt; 0) ancak sonuç görüldükten sonra (<see cref="AdmissionUi.SeenResultFor"/>; ilk K8/K8A varışı, PLAN §1.8), o zamana dek BEKLENİYOR
    /// görünür ve GİDEN'deki yaprak yerinde kalır. <see cref="AdmissionSnapshot.MarkerRule"/> son
    /// yapılan ve talimatta maddesi olan incelemenin madde dizinidir (0 tabanlı; −1 = en üst).
    /// </para>
    /// </summary>
    public static class AdmissionSnapshotBuilder
    {
        // ---------------------------------------------------------------- talep formunun satır düzeni (RequestData.Labels/Values)
        // İki dizi aynı uzunlukta. 0–5 formun altı satırı (etiket | değer; RequestFormFace.RowCount, SupervisorRow 5), 6 başlık | vardiya
        // (RequestFormFace.ShiftIndex), 7–9 yalnız etiket (prefab'da DeskLocText olarak da durur; değer boş).

        /// <summary>AD | belgedeki ad.</summary>
        public const int RequestName = 0;
        /// <summary>BELGE No. | belge numarası.</summary>
        public const int RequestSerial = 1;
        /// <summary>SEVK No. | sevk numarası.</summary>
        public const int RequestReferral = 2;
        /// <summary>MÜHÜR | belgedeki mühür.</summary>
        public const int RequestSeal = 3;
        /// <summary>SORULACAK | "{geldiği yer} kaydı".</summary>
        public const int RequestAsk = 4;
        /// <summary>GÖZETMEN | gözetmenin adı ya da "gözetmen yok · …" (kırmızı: <see cref="RequestData.SupervisorMissing"/>).</summary>
        public const int RequestSupervisor = 5;
        /// <summary>"KYGA · DIŞ KAYIT TALEBİ" | vardiya ("V.005").</summary>
        public const int RequestHeader = 6;
        /// <summary>"(BOŞTA üst kartı)" | boş; gözetmen yoksa etiket de boş.</summary>
        public const int RequestSupervisorNote = 7;
        /// <summary>"sonuç sonraki kapanışta" | boş.</summary>
        public const int RequestFooter = 8;
        /// <summary>Karbon kopyanın köşe yazısı "KOPYA" | boş.</summary>
        public const int RequestCarbon = 9;
        public const int RequestRows = 10;

        /// <summary>Bulgu fişi anahtarları (<see cref="SlipData.Key"/>): aynı anahtarlı fiş ikinci kez ağızdan çıkmaz.</summary>
        public static string CandidateSlipKey(string candidateId, AdmissionTool tool) => "cand:" + candidateId + ":" + AdmissionIds.KindOf(tool);
        public static string ResultSlipKey(string candidateId) => "record:" + candidateId + ":result";
        public static string ReviewSlipKey(int protocolShift) => "review:" + protocolShift.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public static AdmissionSnapshot Build(Simulation sim, AdmissionUi ui)
        {
            ui = ui ?? new AdmissionUi();
            var s = new AdmissionSnapshot { Words = new AdmissionWords() };
            FillWords(s.Words);
            var adm = sim?.Admission;
            s.Active = adm != null && sim.Board != null;
            var c = s.Active ? adm.Candidate : null;
            string id = c?.Resident?.Id;
            s.CandidateId = id;
            s.LockReason = s.Active ? CommandLock(sim) : null;
            s.Locked = s.LockReason != null;
            s.DocumentOnCounter = ui.DocumentOnCounter(id);
            s.CounterHoverTitle = Loc.T("KABUL TEZGÂHI");
            s.CounterHoverLine = c != null ? Loc.F("başvuru · {0}", c.Papers?.Name ?? c.Resident.Name) : Loc.T("başvuru yok");

            if (s.Active)
            {
                var protocol = adm.Protocol;
                var archive = adm.ProtocolArchive;
                if (protocol != null)
                {
                    s.Orders = OrdersOf(protocol, archive.Count > 0 ? archive[archive.Count - 1] : null);
                    s.OrdersUnseen = ui.SeenOrdersVersion > 0 && ui.SeenOrdersVersion < protocol.Version;
                }
                for (int i = archive.Count - 1; i >= 0; i--) s.Archive.Add(OrdersOf(archive[i], i > 0 ? archive[i - 1] : null));
            }
            s.Counter = id != null || s.OrdersUnseen ? CounterState.Down : CounterState.Folded;

            FillTools(s, sim, c);
            FillStamps(s, sim, c);
            if (c != null) s.Document = DocumentOf(c);
            if (s.Active) FillSlips(s, sim, adm, c, ui);
            s.Request = RequestOf(s, sim, c);
            s.BreathFog = id != null && sim.AdmissionFindingCode(id, "breath") == "fog";
            s.MarkerRule = c != null && s.Orders != null ? MarkerOf(c, s.Orders.Version) : -1;
            return s;
        }

        // ---------------------------------------------------------------- kilit

        /// <summary>
        /// Core'un kabul komut denetimi (oyun bitti / baskın / plan kilitli), yalnız Core denetimlerinden okunur: aday varken REDDET'in
        /// denetimi (adayda başka koşulu yok), aday yokken canlı bir dosyanın TAHLİYE denetimi (geçerli dosyada başka koşulu yok).
        /// Kilit yoksa null.
        /// </summary>
        static string CommandLock(Simulation sim)
        {
            var adm = sim.Admission;
            if (adm.Candidate != null) return sim.ApplicantDecisionCheck(AdmissionIds.ChoiceOf(StampChoice.Reject));
            var file = adm.Files.FirstOrDefault(f => !f.Departed && f.Resident != null && f.Resident.Alive);
            return file != null ? sim.ResidentDismissCheck(file.PersonId) : null;
        }

        // ---------------------------------------------------------------- sözcükler

        static void FillWords(AdmissionWords w)
        {
            w.Drawer = Loc.T("EVRAK");
            w.Out = Loc.T("GİDEN · TELSİZ");
            w.Holder = Loc.T("TALİMAT");
            w.Archive = Loc.T("ESKİ");
            w.Slip = Loc.T("BULGU");
            w.Findings = Loc.T("BULGULAR");
            w.Under = Loc.T("KABUL");
            w.Glass = Loc.T("BUĞU CAMI");
            w.Intercom = Loc.T("İNTERKOM");
            w.Desk = Loc.T("MASA");
            w.Rations = Loc.T("ERZAK");
        }

        // ---------------------------------------------------------------- araçlar ve damgalar

        /// <summary>Aracın üzerine gelme başlığı: nesnenin adı ve yaptığı inceleme ("BÜYÜTEÇ · belgeyle karşılaştır").</summary>
        public static string ToolTitle(AdmissionTool tool)
        {
            switch (tool)
            {
                case AdmissionTool.Papers: return Loc.F("{0} · {1}", Loc.T("BÜYÜTEÇ"), Loc.T("belgeyle karşılaştır"));
                case AdmissionTool.Interview: return Loc.F("{0} · {1}", Loc.T("İNTERKOM"), Loc.T("görüşme"));
                case AdmissionTool.Breath: return Loc.F("{0} · {1}", Loc.T("BUĞU CAMI"), Loc.T("soğuk gözlem"));
                case AdmissionTool.Record: return Loc.F("{0} · {1}", Loc.T("TALEP DEFTERİ"), Loc.T("dış kayıt"));
                default: return string.Empty;
            }
        }

        /// <summary>Bulgu fişinin başlığı (eski pencerenin düğme yazıları).</summary>
        public static string SlipTitle(AdmissionTool tool)
        {
            switch (tool)
            {
                case AdmissionTool.Papers: return Loc.T("BELGELERİ KARŞILAŞTIR");
                case AdmissionTool.Interview: return Loc.T("GÖRÜŞME YAP");
                case AdmissionTool.Breath: return Loc.T("SOĞUK GÖZLEM");
                case AdmissionTool.Record: return Loc.T("DIŞ KAYIT TEYİDİ");
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Dört araç (<see cref="AdmissionTool"/> sırası): etkinlik ve neden <see cref="Simulation.ApplicantInspectionCheck"/>'ten aynen.
        /// Tek kısa etiket: dış kayıt gözetmensizken (Boşta'da uygun kişi yok) "gözetmen yok · BOŞTA boş" (R17-not); Core iletisi o zaman
        /// <see cref="RequestData.Reason"/>'da.
        /// </summary>
        static void FillTools(AdmissionSnapshot s, Simulation sim, CommunityApplicant c)
        {
            for (int i = 0; i < s.Tools.Length; i++)
            {
                var tool = (AdmissionTool)i;
                string kind = AdmissionIds.KindOf(tool);
                string why = sim != null ? sim.ApplicantInspectionCheck(kind) : Loc.T("Bu kayıtta topluluk kabulü yok.");
                bool used = c != null && c.Inspections.Contains(kind);
                string reason = why;
                if (tool == AdmissionTool.Record && why != null && c != null && s.LockReason == null && !used && !sim.HasAdmissionWatcher(c.Resident.Id))
                    reason = Loc.T("gözetmen yok · BOŞTA boş");
                s.Tools[i] = new ToolData { Used = used, Enabled = s.Active && why == null, Title = ToolTitle(tool), Reason = reason };
            }
        }

        /// <summary>Raf plakası (iki satır; TXT_S1–4 ile aynı metin).</summary>
        public static string StampPlate(StampChoice choice)
        {
            switch (choice)
            {
                case StampChoice.Admit: return Loc.T("KABUL ·\nTAM ERİŞİM");
                case StampChoice.Restricted: return Loc.T("KISITLI\nKABUL");
                case StampChoice.Reject: return Loc.T("REDDET");
                case StampChoice.Aid: return Loc.T("DIŞARIDAN\nYARDIM");
                default: return string.Empty;
            }
        }

        /// <summary>Damganın sapındaki kısa ad (TXT_Cap). EN'de tasarımın ADMIT ve LIMIT sözcükleri (tezgâh şablonu ve dosyadaki KABUL/KISITLI aynen kalır).</summary>
        public static string StampCap(StampChoice choice)
        {
            switch (choice)
            {
                case StampChoice.Admit: return DeskLoc.Instance.Ctx("stamp", "KABUL");
                case StampChoice.Restricted: return DeskLoc.Instance.Ctx("stamp", "KISITLI");
                case StampChoice.Reject: return Loc.T("RET");
                case StampChoice.Aid: return Loc.T("YARDIM");
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Dört damga (<see cref="StampChoice"/> sırası): etkinlik ve neden <see cref="Simulation.ApplicantDecisionCheck"/>'ten aynen. Kısa
        /// neden yalnız tasarımın iki etiketi için (C §4.4): kapasite "KAPASİTE n/n", erzak "erzak yetersiz"; durum Core'un açık değerlerinden
        /// (canlı sayısı ve kapasite, erzak ve yardım payı) okunur, metinden değil. Öteki nedenler Core iletisiyle.
        /// </summary>
        static void FillStamps(AdmissionSnapshot s, Simulation sim, CommunityApplicant c)
        {
            int alive = s.Active ? sim.Board.People.Count(p => p.Alive) : 0;
            int capacity = sim?.B.boardResidentCapacity ?? 0;
            for (int i = 0; i < s.Stamps.Length; i++)
            {
                var choice = (StampChoice)i;
                string why = sim != null ? sim.ApplicantDecisionCheck(AdmissionIds.ChoiceOf(choice)) : Loc.T("Bu kayıtta topluluk kabulü yok.");
                string shortWhy = null;
                if (why != null && c != null && s.LockReason == null)
                {
                    if ((choice == StampChoice.Admit || choice == StampChoice.Restricted) && alive >= capacity)
                        shortWhy = Loc.F("KAPASİTE {0}/{1}", alive, capacity);
                    else if (choice == StampChoice.Aid && (sim.Shelter?.Supplies ?? 0) < sim.B.admissionAidRations)
                        shortWhy = Loc.T("erzak yetersiz");
                }
                s.Stamps[i] = new StampData
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
                case StampChoice.Restricted: return new[] { Loc.T("KISITLI KABUL"), Loc.F("{0} VARDİYA", b?.admissionRestrictedShifts ?? 0) };
                case StampChoice.Reject: return new[] { Loc.T("REDDET") };
                case StampChoice.Aid: return new[] { Loc.T("DIŞARIDAN YARDIM"), Loc.F("{0} ERZAK", Amount(b?.admissionAidRations ?? 0)) };
                default: return new string[0];
            }
        }

        /// <summary>
        /// Belgeye basılan damganın izini kurar (karar dizisi; komut basış anında yazılır, aday ardından Core'dan düşer): seçim kimliği,
        /// satırlar, renk. <c>SiginakAdmission</c> basıştan önceki anlık görüntünün belgesine uygular.
        /// </summary>
        public static void Stamp(DocumentData d, StampChoice choice, Balance b)
        {
            if (d == null) return;
            d.StampChoiceId = AdmissionIds.ChoiceOf(choice);
            d.StampLines = ImpressionLines(choice, b);
            d.StampColor = StampColorHex(choice);
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

        // ---------------------------------------------------------------- belge

        static DocumentData DocumentOf(CommunityApplicant c)
        {
            var p = c.Papers;
            var r = c.Resident;
            return new DocumentData
            {
                Name = p.Name, Profession = Loc.T(p.Profession), Origin = Loc.T(p.Origin), Issued = ShiftText(p.IssuedShift),
                Serial = p.Serial, Referral = p.ReferralSerial, Seal = p.Seal,
                PortraitKey = PlanningBoardSnapshotBuilder.PortraitKeyOf(p.Name), PortraitFlip = NumberedName(p.Name),
                Stats = Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", r.Hp, r.MaxHp, r.Move, r.Power),
            };
        }

        /// <summary>
        /// Sayılı ad ("Suna 9"): portre anahtarı sayıyı atar (kurucu Suna'nın yüzü); sunum yüzü belirlenimci aynalar (D18-B). Yalnız ada
        /// bakar, gizli kimliğe asla.
        /// </summary>
        public static bool NumberedName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.Trim();
            int space = n.LastIndexOf(' ');
            return space > 0 && space < n.Length - 1 && n.Substring(space + 1).All(char.IsDigit);
        }

        // ---------------------------------------------------------------- talimat şeridi

        /// <summary>
        /// Bir sürümün şeridi (Core metni ve sırası, D6-A). Madde önceki sürümde aynen varsa "sürüm n’den", yoksa (sürüm &gt; 1) "YENİ ·
        /// V.nnn"; karşılaştırma çevrilmemiş Core anahtarlarıyla (sunum etiketi, karar değil). Tarih oyunun kuralıyla V.{IssuedShift+1}.
        /// </summary>
        public static OrdersData OrdersOf(AdmissionProtocol p, AdmissionProtocol previous)
        {
            var o = new OrdersData
            {
                Version = p.Version,
                Header = Loc.T("KYGA · KABUL TALİMATI"),
                Number = Loc.F("No. {0}", p.Version),
                Title = Loc.F("SÜRÜM {0}", p.Version),
                Subtitle = Loc.F("V.{0} geçerli · {1} kural", TrNumber.WithAblative(p.IssuedShift + 1), p.Rules.Count),
                ArchiveTitle = string.Empty, ArchiveRule = string.Empty, Footer = string.Empty,
                Footer2 = Loc.T("KYGA · KAPI"),
            };
            if (previous != null)
            {
                o.ArchiveTitle = Loc.F("ARŞİV · SÜRÜM {0} · V.{1:000}", previous.Version, previous.IssuedShift + 1);
                o.ArchiveRule = previous.Rules.Count > 0 ? Loc.T(previous.Rules[0]) : string.Empty;
                o.Footer = Loc.F("sürüm {0} arşivde", previous.Version);
            }
            for (int i = 0; i < p.Rules.Count; i++)
            {
                string rule = p.Rules[i];
                var r = new RuleData { Number = DeskFormat.Int(i + 1), Text = Loc.T(rule) };
                if (previous != null && previous.Rules.Contains(rule)) r.Tag = Loc.F("sürüm {0}’den", previous.Version);
                else if (p.Version > 1)
                {
                    r.IsNew = true;
                    r.Tag = Loc.F("YENİ · V.{0:000}", p.IssuedShift + 1);
                }
                o.Rules.Add(r);
            }
            return o;
        }

        static int MarkerOf(CommunityApplicant c, int version)
        {
            int item = 0;
            foreach (string kind in c.Inspections)
            {
                int m = kind == "papers" ? AdmissionRuleMap.Papers(version, c.Papers?.Serial)
                    : kind == "breath" ? AdmissionRuleMap.Breath(version)
                    : kind == "record" ? AdmissionRuleMap.Record(version)
                    : 0;
                if (m > 0) item = m;
            }
            return item - 1;
        }

        // ---------------------------------------------------------------- fişler

        static void FillSlips(AdmissionSnapshot s, Simulation sim, CommunityAdmissionState adm, CommunityApplicant c, AdmissionUi ui)
        {
            var review = ReviewSlip(sim, adm);
            if (review != null) s.Slips.Add(review);
            if (c == null) return;
            string id = c.Resident.Id;
            int version = adm.Protocol?.Version ?? 1;
            foreach (string kind in c.Inspections)
            {
                if (!AdmissionIds.TryTool(kind, out var tool)) continue;   // "record_done" araç değil: sonucu "record" fişinde
                var slip = new SlipData
                {
                    Tool = tool, Header = Loc.T("KYGA · İNCELEME BULGUSU"), Shift = ShiftText(c.ArrivalShift), Title = SlipTitle(tool),
                    Icon = kind, Key = CandidateSlipKey(id, tool),
                };
                switch (tool)
                {
                    case AdmissionTool.Papers: PapersSlip(slip, sim, c, version); break;
                    case AdmissionTool.Interview: InterviewSlip(slip, c); break;
                    case AdmissionTool.Breath: BreathSlip(slip, sim.AdmissionFindingCode(id, "breath")); break;
                    case AdmissionTool.Record: RecordSlip(slip, sim, adm, c, HoldsResult(ui, id)); break;
                }
                s.Slips.Add(slip);
            }
        }

        static void PapersSlip(SlipData slip, Simulation sim, CommunityApplicant c, int version)
        {
            var p = c.Papers;
            string code = sim.AdmissionFindingCode(c.Resident.Id, "papers");
            slip.Body = Loc.F("Belge No. {0} · sevk No. {1}. Mühür {2}, düzenleme V.{3:000}.", p.Serial, p.ReferralSerial, p.Seal, p.IssuedShift + 1);
            bool match = code == "match";
            slip.Verdict = match ? Loc.T("EŞLEŞİYOR") : Loc.T("UYUŞMUYOR");
            slip.Kind = match ? SlipVerdict.Match : SlipVerdict.Mismatch;
            int rule = AdmissionRuleMap.Record(version);
            if (AdmissionRuleMap.IsN17(p.Serial) && rule > 0 && !c.Inspections.Contains("record")) slip.Note = Loc.F("N-17: dış kayıt gerekir (madde {0})", rule);
            else if (!match && code != null) slip.Note = PaperPhrase(code);
        }

        static void InterviewSlip(SlipData slip, CommunityApplicant c)
        {
            slip.Body = string.IsNullOrEmpty(c.Interview) ? string.Empty : Loc.T(c.Interview);
            slip.Verdict = Loc.T("GÖRÜŞÜLDÜ");
            slip.Kind = SlipVerdict.Neutral;
        }

        static void BreathSlip(SlipData slip, string code)
        {
            bool fog = code == "fog";
            slip.Body = fog ? Loc.T("Soğuk camda nefes buğusu görüldü.") : Loc.T("Soğuk camda nefes buğusu görülmedi.");
            slip.Note = Loc.T("Buğu tek başına insanlık garantisi değildir.");
            slip.Verdict = fog ? Loc.T("BUĞU VAR") : Loc.T("BUĞU YOK");
            slip.Kind = fog ? SlipVerdict.Neutral : SlipVerdict.Mismatch;
        }

        /// <summary>
        /// Sonuç fişi henüz görülmedi mi: canlı sunum durumunda (<see cref="AdmissionUi.SeenOrdersVersion"/> &gt; 0; 0 = bilinmiyor, testlerin
        /// ve fikstürlerin boş durumu) <see cref="AdmissionUi.SeenResultFor"/> bu aday değilse sonuç ilk K8/K8A varışına dek tutulur (PLAN §1.8:
        /// fişin ağızdan çıkışı orada bir kez; kapanışta K1'den görünmez).
        /// </summary>
        public static bool HoldsResult(AdmissionUi ui, string candidateId) =>
            ui != null && ui.SeenOrdersVersion > 0 && !string.IsNullOrEmpty(candidateId) && ui.SeenResultFor != candidateId;

        /// <summary>
        /// DIŞ KAYIT: sonuç gelmeden BEKLENİYOR (kesikli; gözetmen adı canlı, D2-A), sonuçla KAYIT BULUNDU / KAYIT FARKLI. Talep bekliyorken
        /// gözlem yuvası başka dosyaya geçtiyse (H2, son istek kazanır) ya da Boşta'da uygun kişi kalmadıysa sonuç gelmez: gövde bunu söyler.
        /// <paramref name="holdResult"/>: sonuç geldi ama henüz görülmedi; fiş BEKLENİYOR anahtarıyla kalır (ilk varışta sonuç ağızdan çıkar).
        /// </summary>
        static void RecordSlip(SlipData slip, Simulation sim, CommunityAdmissionState adm, CommunityApplicant c, bool holdResult)
        {
            string id = c.Resident.Id;
            var p = c.Papers;
            bool done = c.Inspections.Contains("record_done");
            if (done && !holdResult)
            {
                string code = sim.AdmissionFindingCode(id, "record");
                bool match = code == "match";
                var result = adm.Events.LastOrDefault(e => e.PersonId == id && e.Kind == "record");
                slip.Shift = ShiftText(result?.Shift ?? sim.S.Day);
                slip.Body = match ? Loc.F("{0} kaydı belgeyle eşleşiyor.", p.Seal) : Loc.F("{0} kaydı belgeden farklı: {1}.", p.Seal, FieldWord(code));
                slip.Verdict = match ? Loc.T("KAYIT BULUNDU") : Loc.T("KAYIT FARKLI");
                slip.Kind = match ? SlipVerdict.Found : SlipVerdict.Differs;
                slip.Key = ResultSlipKey(id);
                return;
            }
            var watcher = sim.AdmissionWatcher(id);
            if (done) slip.Body = watcher != null ? Loc.F("Gözetmen: {0} · {1} kaydı telsizle soruldu.", watcher.Name, p.Seal) : string.Empty;
            else if (adm.ObservationTarget != id) slip.Body = Loc.T("talep iptal edildi · sonuç gelmez");
            else if (watcher == null) slip.Body = Loc.T("gözetmen yok · sonuç gelmez");
            else slip.Body = Loc.F("Gözetmen: {0} · {1} kaydı telsizle soruldu.", watcher.Name, p.Seal);
            slip.Note = Loc.F("sonuç V.{0:000} kapanışında", done ? sim.S.Day : sim.S.Day + 1);   // tutulan sonuç: geldiği kapanış
            slip.Verdict = DeskLoc.Instance.Ctx("slip", "BEKLENİYOR");
            slip.Kind = SlipVerdict.Pending;
            slip.Dashed = true;
        }

        /// <summary>
        /// İNCELEME FİŞİ (R17e): son talimat değişiminin gününde açılan ve hâlâ çözülmemiş <c>file_review</c> olaylarının kişileri (dosyası
        /// <c>ReviewNeeded</c>; en çok üç ad, fazlası "+n"). Dosyalar incelenince kıskaçtan kalkar.
        /// </summary>
        static SlipData ReviewSlip(Simulation sim, CommunityAdmissionState adm)
        {
            var protocolEvent = adm.Events.LastOrDefault(e => e.Kind == "protocol");
            if (protocolEvent == null) return null;
            var names = new List<string>();
            foreach (var e in adm.Events)
            {
                if (e.Kind != "file_review" || e.Shift != protocolEvent.Shift || e.Resolved) continue;
                var f = adm.File(e.PersonId);
                if (f == null || f.Departed || f.Resident == null || !f.Resident.Alive || !f.ReviewNeeded) continue;
                if (!names.Contains(f.Resident.Name)) names.Add(f.Resident.Name);
            }
            if (names.Count == 0) return null;
            int version = VersionAt(adm, protocolEvent.Shift);
            string shown = string.Join(", ", names.Take(3));
            if (names.Count > 3) shown = Loc.F("{0} +{1}", shown, names.Count - 3);
            return new SlipData
            {
                Tool = null, Review = true, Header = Loc.T("KYGA · İNCELEME FİŞİ"), Shift = ShiftText(protocolEvent.Shift),
                Title = Loc.F("TALİMAT {0} · MADDE {1}", version, AdmissionRuleMap.ReviewRule(version)), Icon = "review",
                Body = Loc.F("Eşleşen eski dosya: {0}", shown), Note = Loc.T("Dosyasına İNCELE sekmesi."),
                Verdict = Loc.T("İNCELE"), Kind = SlipVerdict.Review, Key = ReviewSlipKey(protocolEvent.Shift),
            };
        }

        /// <summary>Bir olay vardiyasında yürürlükteki talimat sürümü (arşiv + güncel; bulunmazsa güncel).</summary>
        public static int VersionAt(CommunityAdmissionState adm, int shift)
        {
            if (adm == null) return 1;
            foreach (var p in adm.ProtocolArchive) if (p.IssuedShift == shift) return p.Version;
            return adm.Protocol?.Version ?? 1;
        }

        // ---------------------------------------------------------------- talep formu ve uyarılar

        static RequestData RequestOf(AdmissionSnapshot s, Simulation sim, CommunityApplicant c)
        {
            var r = new RequestData { Labels = new string[RequestRows], Values = new string[RequestRows] };
            for (int i = 0; i < RequestRows; i++) r.Labels[i] = r.Values[i] = string.Empty;
            r.Labels[RequestHeader] = Loc.T("KYGA · DIŞ KAYIT TALEBİ");
            r.Labels[RequestName] = Loc.T("AD");
            r.Labels[RequestSerial] = Loc.T("BELGE No.");
            r.Labels[RequestReferral] = Loc.T("SEVK No.");
            r.Labels[RequestSeal] = Loc.T("MÜHÜR");
            r.Labels[RequestAsk] = Loc.T("SORULACAK");
            r.Labels[RequestSupervisor] = Loc.T("GÖZETMEN");
            r.Labels[RequestFooter] = Loc.T("sonuç sonraki kapanışta");
            r.Labels[RequestCarbon] = Loc.T("KOPYA");
            var tool = s.Tools[(int)AdmissionTool.Record];
            r.Enabled = tool.Enabled;
            if (c == null)
            {
                r.Reason = tool.Reason;
                return r;
            }
            string id = c.Resident.Id;
            var p = c.Papers;
            bool sent = c.Inspections.Contains("record"), done = c.Inspections.Contains("record_done");
            var watcher = sim.AdmissionWatcher(id);
            r.Sent = sent;
            r.Reason = sim.ApplicantInspectionCheck("record");
            r.Supervisor = watcher?.Name;
            r.SupervisorMissing = watcher == null;
            r.Warning = RequestWarning(sim);
            r.Values[RequestHeader] = ShiftText(sim.S.Day);
            r.Values[RequestName] = p.Name;
            r.Values[RequestSerial] = p.Serial;
            r.Values[RequestReferral] = p.ReferralSerial;
            r.Values[RequestSeal] = p.Seal;
            r.Values[RequestAsk] = Loc.F("{0} kaydı", Loc.T(p.Origin));
            if (watcher != null)
            {
                r.Values[RequestSupervisor] = watcher.Name;
                // Aday destede değildir ve emekli görevler Boşta'dadır: adayın gözetmeni Boşta destesinin üst kartıdır.
                r.Labels[RequestSupervisorNote] = Loc.T("(BOŞTA üst kartı)");
            }
            else if (!done) r.Values[RequestSupervisor] = sent ? Loc.T("gözetmen yok · sonuç gelmez") : Loc.T("gözetmen yok · BOŞTA boş");
            return r;
        }

        /// <summary>
        /// Talep defterinin uyarısı (D3-a, H2/H4; tek gözlem yuvası, son istek kazanır). Talepten önce: yuva başka bir dosyanın bekleyen
        /// incelemesindeyse "Gözetmen şu an X dosyasında…", gözetimle çalışan kısıtlı bir kişideyse "X gözetimle çalışıyor…". Talepten sonra:
        /// yuva başka yere geçtiyse sonuç gelmez. Yoksa null. Yalnız açık durumdan okur.
        /// </summary>
        public static string RequestWarning(Simulation sim)
        {
            var adm = sim?.Admission;
            var c = adm?.Candidate;
            if (c == null) return null;
            string id = c.Resident.Id, target = adm.ObservationTarget;
            if (c.Inspections.Contains("record"))
                return !c.Inspections.Contains("record_done") && target != id ? Loc.T("talep iptal edildi · sonuç gelmez") : null;
            return SlotWarning(sim, adm, id);
        }

        /// <summary>
        /// Dosya İNCELE sekmesinin uyarısı (simetrik H2/H4): kapıdaki adayın dış kayıt talebi bekliyorsa "Kapıdaki dış kayıt talebi iptal
        /// olur.", yuva gözetimle çalışan başka bir kısıtlı kişideyse "X gözetimle çalışıyor…". Yoksa null.
        /// </summary>
        public static string ReviewWarning(Simulation sim, string personId)
        {
            var adm = sim?.Admission;
            if (adm == null || string.IsNullOrEmpty(personId)) return null;
            var c = adm.Candidate;
            if (c != null && adm.ObservationTarget == c.Resident.Id && c.Inspections.Contains("record") && !c.Inspections.Contains("record_done"))
                return Loc.T("Kapıdaki dış kayıt talebi iptal olur.");
            return SlotWarning(sim, adm, personId);
        }

        static string SlotWarning(Simulation sim, CommunityAdmissionState adm, string requester)
        {
            string target = adm.ObservationTarget;
            if (string.IsNullOrEmpty(target) || target == requester) return null;
            var f = adm.File(target);
            if (f == null || f.Departed || f.Resident == null || !f.Resident.Alive) return null;
            if (f.ReviewPending) return Loc.F("Gözetmen şu an {0} dosyasında; bu talep onu iptal eder.", f.Resident.Name);
            if (f.Resident.Restricted && f.Resident.Job != "rest" && sim.CanRestrictedResidentWork(target))
                return Loc.F("{0} gözetimle çalışıyor; bu talep onun planını bozar.", f.Resident.Name);
            return null;
        }

        // ---------------------------------------------------------------- bulgu ifadeleri (kişi dosyası da kullanır)

        /// <summary>Belge/dış kayıt kodunun sunum ifadesi (D5: "robot" yok; Core metni değil).</summary>
        public static string PaperPhrase(string code)
        {
            switch (code)
            {
                case "match": return Loc.T("Belge alanları uyumlu");
                case "date": return Loc.T("Düzenleme tarihi kayıttan farklı");
                case "serial": return Loc.T("Belge No. ≠ sevk No.");
                case "seal": return Loc.T("Mühür hattın örneğiyle uyuşmuyor");
                case "origin": return Loc.T("Geldiği yer kayıttan farklı");
                default: return null;
            }
        }

        /// <summary>Soğuk gözlem kodunun sunum ifadesi.</summary>
        public static string BreathPhrase(string code) =>
            code == "fog" ? Loc.T("Soğuk gözlem: buğu var") : code == "clear" ? Loc.T("Soğuk gözlem: buğu yok") : null;

        /// <summary>Dış kayıt sonucunda farklı çıkan alanın adı (cümle içi).</summary>
        static string FieldWord(string code)
        {
            switch (code)
            {
                case "date": return Loc.T("düzenleme tarihi");
                case "serial": return Loc.T("sevk numarası");
                case "seal": return Loc.T("mühür");
                case "origin": return Loc.T("geldiği yer");
                default: return string.Empty;
            }
        }

        static string ShiftText(int day) => Loc.F("V.{0:000}", day + 1);
    }

    /// <summary>
    /// Talimat maddesi eşlemesi (sunum tablosu; Core kural sırasına bağlı, PLAN §1.9; <c>Rev3AdmissionSnapshotTests</c> Core'un kural
    /// sayısını ve metinlerini kilitler). Değerler 1 tabanlı madde numarasıdır; 0 = maddesi yok (işaretçi en üstte, yakut sönük).
    /// v1: 1 belge (tarih/numara), 2 belge kusuru · v2: 1 N-17, 2 S-6 buğu, 3 birlikte değerlendir · v3: 1 N-17, 2 R-4 buğu, 3 erişim/parça.
    /// </summary>
    public static class AdmissionRuleMap
    {
        /// <summary>Sürüm 2'de çalınan boş sevk belgelerinin serisi.</summary>
        public static bool IsN17(string serial) => serial != null && serial.StartsWith("N-17/", StringComparison.Ordinal);

        /// <summary>
        /// Belge karşılaştırması: v1 madde 1; v2/v3'te N-17 serisi N-17 maddesi (1); değilse v2 madde 3 (belge kusuru, dış kayıt, iz
        /// birlikte), v3'te maddesi yok (v3 madde 3 erişim/parça kayıtları; eleştiri 16).
        /// </summary>
        public static int Papers(int version, string serial)
        {
            if (version <= 1) return 1;
            if (IsN17(serial)) return 1;
            return version == 2 ? 3 : 0;
        }

        /// <summary>Soğuk gözlem: v2 madde 2 (S-6), v3 madde 2 (R-4); v1'de maddesi yok.</summary>
        public static int Breath(int version) => version >= 2 ? 2 : 0;

        /// <summary>Dış kayıt: N-17 maddesi (v2/v3 madde 1); v1'de maddesi yok.</summary>
        public static int Record(int version) => version >= 2 ? 1 : 0;

        /// <summary>İNCELEME FİŞİ'nin maddesi: v2 N-17 (1), v3 buğu (2).</summary>
        public static int ReviewRule(int version) => version == 2 ? 1 : version >= 3 ? 2 : 0;
    }
}
