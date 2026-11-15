using System.Collections.Generic;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Olay tutanağının ve olay göstergelerinin metinleri simülasyondan (KOS aşama 4, tasarım §7). Saf okuma: Core durumunu
    /// değiştirmez, RNG çekmez, komut yazmaz; yalnız public okuyucuları kullanır (<c>PendingShelterEvent</c>,
    /// <c>ShelterEventBody</c>, <c>ShelterEventOptions</c>, <c>ShelterEventDecisionCheck</c>, <c>ShelterEventPrintCheck</c>,
    /// <c>ShelterEvents.Log</c>, tayın ve karartma okuyucuları). Gizli kimlik okunmaz: iz kâğıdı insan ve sızmacı için aynıdır.
    /// </summary>
    public static class ShelterEventSnapshotBuilder
    {
        /// <summary>Açık olayın "aynı kâğıt" anahtarı; olay yoksa null.</summary>
        public static string KeyOf(ShelterEventInstance e) =>
            e == null ? null : string.Join("|", e.Kind, e.OpenedShift.ToString(System.Globalization.CultureInfo.InvariantCulture), e.PersonId ?? "", e.OtherId ?? "");

        /// <summary>
        /// Kâğıda yazılacak açık olay: olaylar kapalıysa, olay yoksa, harekât bittiyse ya da olay yetimse (hedefi sığınakta değil;
        /// Core onu kapanışta kendiliğinden kapatır) null.
        /// </summary>
        public static ShelterEventInstance Visible(Simulation sim)
        {
            var e = sim?.Board != null ? sim.PendingShelterEvent : null;
            if (e == null || sim.S.Ending != null || Orphan(sim, e)) return null;
            return e;
        }

        /// <summary>Hedefi giden olay (Core'un <c>CloseOrphanShelterEvent</c> kuralı).</summary>
        public static bool Orphan(Simulation sim, ShelterEventInstance e)
        {
            if (e == null || !ShelterEventData.NeedsLivingTarget(e.Kind)) return false;
            return !(InShelter(sim, e.PersonId) && (e.Kind != "kavga" || InShelter(sim, e.OtherId)));
        }

        static bool InShelter(Simulation sim, string id)
        {
            var p = string.IsNullOrEmpty(id) ? null : sim.Board.Person(id);
            return p != null && p.Alive;
        }

        /// <summary>Kişinin adı: panoda, değilse dosyada (anma: ölü kişi), değilse kimlik.</summary>
        public static string NameOf(Simulation sim, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return sim.Board?.Person(id)?.Name ?? sim.Admission?.File(id)?.Resident?.Name ?? id;
        }

        /// <summary>
        /// Tutanağın anlık görüntüsü; gösterilecek olay yoksa null. <paramref name="storyHold"/>: açılış belgesi açık (karar verilemez).
        /// Seçim ve son ret <paramref name="ui"/>'dan, yalnız aynı kâğıdın anahtarıyla.
        /// </summary>
        public static ShelterEventPaperData Build(Simulation sim, ShelterEventUi ui, bool storyHold = false)
        {
            var e = Visible(sim);
            if (e == null) return null;
            string key = KeyOf(e);
            bool same = ui != null && ui.ForKey == key;
            var d = new ShelterEventPaperData
            {
                Key = key,
                Kind = e.Kind,
                Shift = Loc.F("V.{0:000}", e.OpenedShift + 1),
                Title = ShelterEventData.Title(e.Kind),
                Body = sim.ShelterEventBody(e),
            };
            if (e.Kind == "kavga") d.Person = Loc.F("KİŞİLER · {0} · {1}", NameOf(sim, e.PersonId), NameOf(sim, e.OtherId));
            else if (!string.IsNullOrEmpty(e.PersonId)) d.Person = Loc.F("KİŞİ · {0}", NameOf(sim, e.PersonId));

            var options = sim.ShelterEventOptions();
            ShelterEventOption probe = null;
            foreach (var o in options) if (o.Open) { probe = o; break; }
            // Genel kilit: seçenekten bağımsız denetim (oyun bitti, baskın sürüyor, fiş basıldı); açılış belgesi açıksa o
            string general = storyHold ? Loc.T("Önce açılış belgesini kapat.") : probe != null ? sim.ShelterEventDecisionCheck(probe.Id) : null;
            int n = 0;
            ShelterEventRowData selected = null;
            foreach (var o in options)
            {
                if (n >= ShelterEventPaperData.MaxRows) break;
                n++;
                var row = new ShelterEventRowData
                {
                    Id = o.Id,
                    Number = n.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Name = Loc.F("{0} · {1}", n, o.Name),
                    SafeTag = o.Safe ? ThresholdWarn(sim, o.Id) ? Loc.T("EŞİK") : Loc.T("GÜVENLİ") : null,
                    SafeWarn = o.Safe && ThresholdWarn(sim, o.Id),
                    Cost = ExposureCostPreview(sim, o.Id, o.Cost),
                    Risk = string.IsNullOrEmpty(o.Risk) ? null : Loc.F("Risk: {0}", o.Risk),
                    KitLock = string.IsNullOrEmpty(o.KitLock) ? null : o.KitLock,
                    Closed = o.Open ? null : Loc.F("KAPALI · {0}", o.ClosedReason),
                    ClosedStamp = o.Open ? null : Loc.T("KAPALI"),
                    Safe = o.Safe,
                    Enabled = o.Open && general == null,
                };
                row.Selected = same && row.Enabled && ui.SelectedId == o.Id;
                if (row.Selected) selected = row;
                d.Rows.Add(row);
            }
            d.ConfirmReady = selected != null;
            // İpucu yalnız açık satırların numaralarını sayar (kapalı satır seçilemez); açık satır yoksa (genel kilit) karar verilemez
            var openRows = new List<string>();
            foreach (var r in d.Rows) if (r.Enabled) openRows.Add(r.Number);
            d.Confirm = selected != null
                ? Loc.F("{0} · damgala", options.Find(o => o.Id == selected.Id).Name)
                : openRows.Count > 0 ? Loc.F("önce bir satır seç ({0})", string.Join(", ", openRows)) : Loc.T("karar verilemez");
            d.Note = general ?? (same ? ui.LastDenied : null);
            d.HoverLine = general ?? Loc.T("Karar bekliyor: al, bir satır seç, damgala.");
            return d;
        }

        /// <summary>Karar izinin satırları: "KARARA BAĞLANDI" ve seçeneğin adı.</summary>
        public static string[] StampLines(string kind, string optionId) =>
            new[] { Loc.T("KARARA BAĞLANDI"), ShelterEventData.OptionName(kind, optionId) };

        /// <summary>
        /// Fişin olay kilidi (şalter plakasının üzerine gelme satırı ve ÖN KONTROL): açık olayın Core gerekçesi ve tutanağın yeri ("Tutanak
        /// EVRAK tepsisinde."; kayıttan devamda OLAY teleksi yeniden basılmaz, oyuncu kâğıdı buradan bulur); yetim olayda null (Core
        /// <c>PrintShift</c> onu kapatıp geçer, sunum yanlış kilit göstermesin).
        /// </summary>
        public static string PrintIssue(Simulation sim)
        {
            var e = sim?.Board != null ? sim.PendingShelterEvent : null;
            if (e == null || Orphan(sim, e)) return null;
            string why = sim.ShelterEventPrintCheck();
            return why == null ? null : Loc.F("{0} Tutanak EVRAK tepsisinde.", why);
        }

        // ---------------------------------------------------------------- göstergeler

        /// <summary>Olaylar açık ve pano var (göstergeler yalnız o zaman yazılır).</summary>
        static bool On(Simulation sim) => sim?.Board != null && sim.Shelter != null && sim.B.shelterEventsEnabled;

        /// <summary>Durum etiketleri: aç vardiya ("AÇ 2"), yarım tayın ("YARIM TAYIN 3 V"), karartma ("KARARTMA"); yoksa null.</summary>
        public static (string hungry, string half, string blackout) StatusTags(Simulation sim)
        {
            if (!On(sim)) return (null, null, null);
            string hungry = sim.Shelter.HungryStreak > 0 ? Loc.F("AÇ {0}", sim.Shelter.HungryStreak) : null;
            string half = sim.HalfRationActive ? Loc.F("YARIM TAYIN {0} V", sim.HalfRationShiftsLeft) : null;
            string blackout = sim.BlackoutActive ? Loc.T("KARARTMA") : null;
            return (hungry, half, blackout);
        }

        /// <summary>Etiketlerin " · " ile birleşimi (kenar satırı, pano başlığı, CRT ipucu); yoksa null.</summary>
        public static string StatusLine(Simulation sim)
        {
            var (hungry, half, blackout) = StatusTags(sim);
            string line = null;
            foreach (var part in new[] { hungry, half, blackout })
            {
                if (string.IsNullOrEmpty(part)) continue;
                line = line == null ? part : Loc.F("{0} · {1}", line, part);
            }
            return line;
        }

        /// <summary>ERZAK sayacının dinlenme etiketi: ilk etiket (aç &gt; yarım tayın &gt; karartma) ve açlıkta uyarı rengi; yoksa null.</summary>
        public static string CounterTag(Simulation sim, out bool alarm)
        {
            var (hungry, half, blackout) = StatusTags(sim);
            alarm = hungry != null;
            return hungry ?? half ?? blackout;
        }

        /// <summary>CRT ipucu: karartmada açıklamalı satır, değilse durum satırı; yoksa null.</summary>
        public static string CrtHint(Simulation sim)
        {
            if (!On(sim)) return null;
            // KOS aşama 6: konum ve ifşa eşiği önce, sonra sığınak eksik sayacı, sonra karartma ve açlık
            if (sim.Shelter.FallPending && sim.LocationCompromised)
                return sim.FallShiftsLeft > 1 ? Loc.F("KONUM ÇÖZÜLDÜ · {0} VARDİYA KALDI", sim.FallShiftsLeft) : Loc.T("KONUM ÇÖZÜLDÜ · SON VARDİYA");
            if (sim.LocationCompromised) return Loc.T("İFŞA EŞİKTE · KAPANIŞTA KONUM ÇÖZÜLÜR");
            if (sim.UnderstaffedCounting) return Loc.F("SIĞINAK EKSİK {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts);
            if (sim.BlackoutActive)
            {
                var (hungry, half, _) = StatusTags(sim);
                string line = Loc.T("KARARTMA · üretim ve mutfak durdu");
                foreach (var part in new[] { hungry, half })
                    if (!string.IsNullOrEmpty(part)) line = Loc.F("{0} · {1}", line, part);
                return line;
            }
            return StatusLine(sim);
        }

        // ---------------------------------------------------------------- ifşa (KOS aşama 6)

        /// <summary>İFŞA şeridi görünür mü: olaylar açık kartlı oyun ve eşik açık.</summary>
        public static bool ExposureVisible(Simulation sim) => On(sim) && sim.B.exposureThreshold > 0;

        /// <summary>KONUM lambasının seviyesi: 2 konum çözüldü (son vardiya), 1 eşikte (kapanışta çözülür), 0 değil; görünmüyorsa 0.</summary>
        public static int ExposureLevel(Simulation sim)
        {
            if (!ExposureVisible(sim)) return 0;
            return ExposurePanel.BeaconFor(sim.Shelter.Exposure, sim.B.exposureThreshold, sim.Shelter.FallPending);
        }

        /// <summary>
        /// Son şalter vardiya bakımından açık mı (Core'un <c>lastSwitchMinShift</c> kuralı; <see cref="Simulation.LastSwitchCheck"/>'in
        /// kalıcı gerekçesi). Mühürlüyken "konum çözüldü" metinleri son şalteri seçenek olarak önermez. <paramref name="tutorialSeal"/>:
        /// sunumun girdisi, rehber sürüyor ve şalter "mühürlü · rehber" (<c>LastSwitchViewBuilder.LockInputs.Tutorial</c> gibi).
        /// </summary>
        public static bool SwitchOpenByShift(Simulation sim, bool tutorialSeal = false) =>
            !tutorialSeal && sim != null && sim.S.Day + 1 >= sim.B.lastSwitchMinShift;

        /// <summary>İfşanın değeri: eşiğe kadar "E/T", eşiği aşınca "E (eşik T)".</summary>
        static string ExposureValue(int e, int t) => e <= t ? Loc.F("{0}/{1}", e, t) : Loc.F("{0} (eşik {1})", e, t);

        /// <summary>İFŞA şeridinin üzerine gelme satırı (üç durum); sayılar Balance'tan. Görünmüyorsa null.</summary>
        public static string ExposureHint(Simulation sim, bool tutorialSeal = false)
        {
            if (!ExposureVisible(sim)) return null;
            var B = sim.B;
            int e = sim.Shelter.Exposure, t = B.exposureThreshold;
            int left = sim.FallShiftsLeft;
            if (sim.Shelter.FallPending)
            {
                if (left > 1)
                    return SwitchOpenByShift(sim, tutorialSeal)
                        ? Loc.F("KONUM ÇÖZÜLDÜ · bu vardiya dahil {0} vardiya: son baskın, sorgu ya da son şalter.", left)
                        : Loc.F("KONUM ÇÖZÜLDÜ · bu vardiya dahil {0} vardiya: son baskın ya da sorgu.", left);
                return SwitchOpenByShift(sim, tutorialSeal)
                    ? Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın, sorgu ya da son şalter.")
                    : Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın ya da sorgu.");
            }
            if (e >= t)
                return left > 1
                    ? Loc.F("İfşa {0}: eşikte. Bu kapanışta konum çözülür; ifşa düşmezse sığınak ondan {1} kapanış sonra düşer.", ExposureValue(e, t), left)
                    : Loc.F("İfşa {0}: eşikte. Bu kapanışta konum çözülür; sonraki kapanışa kadar ifşa düşmezse sığınak düşer.", ExposureValue(e, t));
            return Loc.F("İfşa {0}. Görmezden gelinen iz {1}, sabotaj {2}, sorgu {3}. Teleks uyarısı: {4} ve {5}. Eşik {6}: konum çözülür; izle varılırsa {9} vardiyan, sabotajla bir vardiyan kalır. {7} ve üstünde alarm her vardiya {8} fazla artar.",
                ExposureValue(e, t), DeskFormat.Signed(B.exposureIgnoredTrace, 0), DeskFormat.Signed(B.exposureSabotage, 0),
                DeskFormat.Signed(B.exposureInterrogation, 0), B.exposureWarnAt1, B.exposureWarnAt2, t, B.exposureAlarmAt,
                DeskFormat.Signed(B.exposureAlarmBonus, 1), System.Math.Max(1, B.exposureDecisionWarnShifts));
        }

        /// <summary>
        /// "Görmezden gel" ifşayı eşiğe vardırır (kâğıtta GÜVENLİ yerine EŞİK): konum henüz çözülmemiş, ifşa eşiğin altında ve iz bedeli
        /// eşiğe ulaştırıyor. Yalnız public durumdan türer.
        /// </summary>
        public static bool ThresholdWarn(Simulation sim, string optionId)
        {
            if (optionId != "yoksay" || !ExposureVisible(sim) || sim.LocationCompromised) return false;
            int e = sim.Shelter.Exposure, t = sim.B.exposureThreshold;
            return e < t && e + sim.B.exposureIgnoredTrace >= t;
        }

        /// <summary>
        /// İfşayı değiştiren kararın şerit önizlemesi (A4 kabul ölçütü: kâğıttaki "İfşa +n" bedeli göstergede okunur; kâğıt elde şeridi
        /// örttüğü için değer satırın kendisinde): "İfşa 2/6 → 3/6." ve alarm ara bedelinin sınırı geçiliyorsa "Alarm her vardiya +0,5."
        /// ya da "Alarm ara bedeli kalkar." Yalnız public durumdan ve Balance'tan; iz insan ve sızmacı için aynı bedeli yazdığından gizli
        /// kimlik okunmaz. İfşayı değiştirmeyen seçenekte ya da şerit görünmüyorken null.
        /// </summary>
        public static string ExposurePreview(Simulation sim, string optionId)
        {
            if (!ExposureVisible(sim)) return null;
            var B = sim.B;
            int delta = optionId == "yoksay" ? B.exposureIgnoredTrace : optionId == "sorgula" ? B.exposureInterrogation : 0;
            if (delta == 0) return null;
            int e = sim.Shelter.Exposure, t = B.exposureThreshold, after = System.Math.Max(0, e + delta);
            string line = Loc.F("İfşa {0} → {1}.", ExposureValue(e, t), ExposureValue(after, t));
            int at = B.exposureAlarmAt;
            if (at > 0 && B.exposureAlarmBonus > 0)
            {
                if (e < at && after >= at) line = Loc.F("{0} Alarm her vardiya {1}.", line, DeskFormat.Signed(B.exposureAlarmBonus, 1));
                else if (e >= at && after < at) line = Loc.F("{0} Alarm ara bedeli kalkar.", line);
            }
            return line;
        }

        /// <summary>Seçeneğin bedel satırı ve (varsa) şerit önizlemesi alt satırda.</summary>
        static string ExposureCostPreview(Simulation sim, string optionId, string cost)
        {
            string preview = ExposurePreview(sim, optionId);
            if (preview == null) return cost;
            return string.IsNullOrEmpty(cost) ? preview : Loc.F("{0}\n{1}", cost, preview);
        }

        /// <summary>Sığınak eksik sayacının satırı (fiş, CRT): "SIĞINAK EKSİK n/m"; sayaç işlemiyorsa null.</summary>
        public static string ShelterShortLine(Simulation sim) => On(sim) && sim.UnderstaffedCounting
            ? Loc.F("SIĞINAK EKSİK {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts) : null;

        // ---------------------------------------------------------------- teleks

        /// <summary>Teleks satırı: durum sözcüğü, ileti ve tür.</summary>
        public readonly struct TelexEntry
        {
            public readonly string Status, Message;
            public readonly TelexKind Kind;
            public TelexEntry(string status, string message, TelexKind kind) { Status = status; Message = message; Kind = kind; }
        }

        /// <summary>Yeni olay duyurusu (OLAY satırı).</summary>
        public static TelexEntry Announcement(ShelterEventInstance e) =>
            new TelexEntry(Loc.T("OLAY"), Loc.F("{0} · tutanak EVRAK tepsisinde. Karar verilmeden vardiya kapanmaz.", ShelterEventData.Title(e.Kind)), TelexKind.Alarm);

        /// <summary>
        /// Günlüğün <paramref name="from"/>'dan sonraki girdileri teleks satırı olarak (o anki dilde); <paramref name="next"/> günlüğün
        /// yeni uzunluğu. İfşa (aşama 6) ve bilinmeyen girdiler atlanır.
        /// </summary>
        public static List<TelexEntry> TelexLines(Simulation sim, int from, out int next, bool tutorialSeal = false)
        {
            var list = new List<TelexEntry>();
            var log = sim?.ShelterEvents?.Log;
            next = log?.Count ?? 0;
            if (log == null) return list;
            // İfşa günlükte yazılmaz; her "ifsa" girdisinden sonraki değer gerçek değişimlerin toplamıdır (ifşa yalnız ChangeExposure ile
            // değişir, günlük kırpılmaz)
            int exposure = 0;
            for (int i = 0; i < System.Math.Max(0, from) && i < log.Count; i++)
                if (log[i].Kind == "ifsa") exposure += (int)log[i].Amount;
            for (int i = System.Math.Max(0, from); i < log.Count; i++)
            {
                if (log[i].Kind == "ifsa") exposure += (int)log[i].Amount;
                var entry = TelexLine(sim, log[i], exposure, tutorialSeal);
                if (entry.HasValue) list.Add(entry.Value);
            }
            return list;
        }

        /// <summary>İfşa kaynağının teleks adı.</summary>
        public static string ExposureSource(string cause) => cause switch
        {
            "iz" => Loc.T("görmezden gelinen iz"),
            "ikinci_iz" => Loc.T("görmezden gelinen ikinci iz"),
            "sabotaj" => Loc.T("sabotaj"),
            "sorgula" => Loc.T("sorgu"),
            _ => Loc.T("başka kaynak"),
        };

        /// <summary>Tek günlük girdisinin satırı; gösterilmeyen girdide null. <paramref name="exposureAfter"/>: "ifsa" girdisinden sonraki ifşa.</summary>
        public static TelexEntry? TelexLine(Simulation sim, ShelterEventLogEntry g, int exposureAfter = -1, bool tutorialSeal = false)
        {
            if (g == null || string.IsNullOrEmpty(g.Kind)) return null;
            if (g.Kind == "ifsa") return ExposureTelex(sim, g, exposureAfter);
            if (g.Kind == "konum")
            {
                // Günlüğün miktarı uyarının süresi (vardiya); eski girdilerde 0 = bir vardiya
                int shifts = (int)g.Amount;
                if (shifts > 1)
                    return new TelexEntry(Loc.T("KONUM ÇÖZÜLDÜ"), SwitchOpenByShift(sim, tutorialSeal)
                        ? Loc.F("YZ sığınağın yerini buldu. {0} vardiyan var: son baskın, sorgu ya da son şalter.", shifts)
                        : Loc.F("YZ sığınağın yerini buldu. {0} vardiyan var: son baskın ya da sorgu.", shifts), TelexKind.Alarm);
                return new TelexEntry(Loc.T("KONUM ÇÖZÜLDÜ"), SwitchOpenByShift(sim, tutorialSeal)
                    ? Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın, sorgu ya da son şalter.")
                    : Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın ya da sorgu."), TelexKind.Alarm);
            }
            if (g.Kind == "konum_kalkti")
                return new TelexEntry(Loc.T("İFŞA"), Loc.T("Sorgu izi sildi: ifşa eşiğin altında, konum uyarısı kalktı."), TelexKind.Done);
            string name = NameOf(sim, g.PersonId);
            switch (g.Kind)
            {
                case "bulgu":
                    return new TelexEntry(Loc.T("TECRİT"), Loc.F("{0}: tecritte bulgu çıktı; sökülerek sığınaktan çıkarıldı.", name), TelexKind.Alarm);
                case "bulgu_yok":
                    return new TelexEntry(Loc.T("TECRİT"), Loc.F("{0}: tecrit bitti, bulgu yok. Görev alabilir.", name), TelexKind.Info);
                case "sokuldu":
                    return new TelexEntry(Loc.T("TECRİT"), Loc.F("{0}: sorgu bitti; sökülerek sığınaktan çıkarıldı.", name), TelexKind.Alarm);
                case "ayrilma":
                    return new TelexEntry(Loc.T("ERZAK"), Loc.F("{0} açlıktan sığınağı terk etti.", name), TelexKind.Alarm);
                case "kapandi":
                    return new TelexEntry(Loc.T("OLAY"), Loc.F("{0} tutanağı kapandı: {1} artık sığınakta değil.", ShelterEventData.Title(g.Option), name), TelexKind.Info);
                case "vadeli":
                    switch (g.Option)
                    {
                        case "yatis_uzar": return new TelexEntry(Loc.T("OLAY"), Loc.F("{0}: yarası kötüleşti, bir vardiya daha yatacak.", name), TelexKind.Info);
                        case "kavga_yara": return new TelexEntry(Loc.T("OLAY"), Loc.F("{0} kavgada yaralandı: bir vardiya yatacak.", name), TelexKind.Info);
                        case "yas": return new TelexEntry(Loc.T("OLAY"), Loc.F("{0} yasta: bu vardiya görev alamaz.", name), TelexKind.Info);
                        case "cagri_guven": return new TelexEntry(Loc.T("OLAY"), Loc.T("Özgür bölge yardımsız kaldı: güven −1."), TelexKind.Info);
                        default: return null;
                    }
                default:
                    if (ShelterEventData.Options(g.Kind).Length == 0) return null;
                    return new TelexEntry(Loc.T("KARAR"), Loc.F("{0}: {1}", ShelterEventData.Title(g.Kind), ShelterEventData.OptionName(g.Kind, g.Option)), TelexKind.Done);
            }
        }

        /// <summary>
        /// İfşa girdisinin satırı: eşik geçilmediyse değer ve kaynak (bilgi); <c>exposureWarnAt1</c>, <c>exposureWarnAt2</c> ve eşiğe
        /// varışta uyarı (alarm). Kişinin adı yazılmaz (yalnız kaynak). <paramref name="after"/> &lt; 0 ise değer bilinmez (yalnız kaynak).
        /// </summary>
        static TelexEntry? ExposureTelex(Simulation sim, ShelterEventLogEntry g, int after)
        {
            if (sim == null) return null;
            int delta = (int)g.Amount, t = sim.B.exposureThreshold;
            if (delta == 0) return null;
            string status = Loc.T("İFŞA");
            if (after < 0) return new TelexEntry(status, Loc.F("{0} {1}", ExposureSource(g.Option), DeskFormat.Signed(delta, 0)), TelexKind.Info);
            int before = after - delta;
            string value = ExposureValue(after, t);
            if (delta > 0 && t > 0)
            {
                if (before < t && after >= t) return new TelexEntry(status, Loc.F("{0}: eşikte. Bu kapanışta konum çözülür.", value), TelexKind.Alarm);
                int w2 = sim.B.exposureWarnAt2, w1 = sim.B.exposureWarnAt1;
                if (w2 > 0 && before < w2 && after >= w2) return new TelexEntry(status, Loc.F("{0}: arama daralıyor; {1}'da konum çözülür.", value, t), TelexKind.Alarm);
                if (w1 > 0 && before < w1 && after >= w1) return new TelexEntry(status, Loc.F("{0}: YZ dinleme istasyonları sığınağın yayınlarını arıyor.", value), TelexKind.Alarm);
            }
            return new TelexEntry(status, Loc.F("{0} ({1} {2})", value, ExposureSource(g.Option), DeskFormat.Signed(delta, 0)), TelexKind.Info);
        }
    }
}
