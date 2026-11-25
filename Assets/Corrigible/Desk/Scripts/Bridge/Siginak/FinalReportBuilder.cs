using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Harekât sonu raporunun içeriği (Revizyon 2 · S8; görünüm <see cref="FinalReportView"/>). Simülasyonu yalnız okur.
    /// İşgal şeridi <c>S.Hist</c>'ten (x = vardiya 1…N; harekât tam işgalle başlar, bugünkü oranla biter). Kırılma anları
    /// sunumda türetilir (en çok altı): ilk özgür bölge, karşı saldırılar (<c>S.Moments</c>), öteki özgür bölgeler (baskın
    /// kaydı), kaybın nedeni (Dağılma'da son güven düşüşü: kapı ya da baskın) ve son olay (son sunucu, güvenin sıfırlanması ya
    /// da ölümcül kapı). Vardiya = gün + 1 (VARDİYA sayacı).
    /// </summary>
    public static class FinalReportBuilder
    {
        /// <summary>Kırılma anlarının en çoğu.</summary>
        public const int MaxMoments = 6;

        static string Name(Simulation sim, string region) => sim.RegionName(region);


        static string Code(int day) => Loc.F("V.{0:000}", day + 1);

        // Kâğıdın yerleşim ölçüleri temada (finalReport.*); tema yoksa yanlarındaki değerler
        static int Measure(string key, int fallback) => SiginakTheme.Active != null ? (int)SiginakTheme.Active.NumberOf(key, fallback) : fallback;

        /// <summary>SİCİL'de ad listelerinin en çok karakteri ("… +n" kesimi; <c>finalReport.record.namesMaxChars</c>).</summary>
        public static int RecordNamesMaxChars => Measure("finalReport.record.namesMaxChars", 150);
        /// <summary>SİCİL'de yollanan insanların ad listesinin en çok karakteri (<c>finalReport.record.doorNamesMaxChars</c>).</summary>
        public static int DoorNamesMaxChars => Measure("finalReport.record.doorNamesMaxChars", 90);
        /// <summary>SİCİL'de ifşa adımlarının en çok satırı (fazlası "… +n"; ilk ve son korunur; <c>finalReport.record.exposureLines</c>).</summary>
        public static int MaxExposureLines => Measure("finalReport.record.exposureLines", 8);
        /// <summary>SİCİL'de ortaya çıkmamış sızmacıların en çok satırı (<c>finalReport.record.hiddenLines</c>).</summary>
        public static int MaxHiddenLines => Measure("finalReport.record.hiddenLines", 4);
        /// <summary>Ton satırındaki ad listesinin en çok karakteri (<c>finalReport.tone.namesMaxChars</c>).</summary>
        public static int ToneNamesMaxChars => Measure("finalReport.tone.namesMaxChars", 80);

        /// <summary>Core'un bugün yazdığı ifşa kaynakları (<c>ChangeExposure</c>); testler bunu Core'la karşılaştırır.</summary>
        public static readonly string[] KnownExposureCauses = { "iz", "ikinci_iz", "sabotaj", "sorgula" };

        /// <summary>
        /// Raporun verisi; son yokken null. KOS aşama 6: sonuç (<see cref="FinalOutcome"/>), çekilme / tahliye damgası, ton satırı,
        /// ölçüt kutusu ve SİCİL sekmesi. Gizli bilgi (<c>EndReport</c>, <c>VictoryTone</c>) yalnız burada ve yalnız son varken okunur.
        /// </summary>
        public static FinalReportData Build(Simulation sim)
        {
            var S = sim.S;
            if (S.Ending == null) return null;
            var end = GameData.Ending(S.Ending);
            var r = sim.EndReport;
            bool won = S.Ending == "silme";
            bool salter = S.Ending == "salter";
            string kind = salter ? r?.SwitchKind ?? sim.Shelter?.SwitchKind : null;
            bool evac = salter && kind == "tahliye";
            var reasons = salter ? (r?.SwitchReasons ?? sim.Shelter?.SwitchReasons ?? new List<string>()) : new List<string>();
            var outcome = won ? FinalOutcome.Won : salter && !evac ? FinalOutcome.Withdrawn : FinalOutcome.Lost;
            int shifts = S.Day + 1;
            var regions = S.Liberation.Regions;
            int free = regions.Count(x => x.Completed), sectors = regions.Sum(x => x.Sectors.Count), captured = regions.Sum(x => x.CapturedCount);
            // Tesis aşama 11 (K39-11): savunma "baskın" sayısına ve başarıya girmez
            int raids = S.RaidLog.Count(l => !FacilityDefenseText.IsDefense(l)), wins = S.RaidLog.Count(l => l.Success && !FacilityDefenseText.IsDefense(l));
            double now = Js.Round(sim.Occupation * 10) / 10;

            var d = new FinalReportData
            {
                Head = Loc.T("KYGA · HAREKÂT SONU RAPORU"),
                Number = Loc.F("SIĞINAK 07 · V.{0:000} · tek nüsha", shifts),
                Outcome = outcome,
                Stamp = outcome == FinalOutcome.Won ? Loc.T("KAZANILDI") : outcome == FinalOutcome.Withdrawn ? Loc.T("ÇEKİLDİ")
                    : evac ? LastSwitchViewBuilder.EvacuatedStamp : Loc.T("KAYBEDİLDİ"),
                Reason = ReasonStamp(S.Ending, r, evac, reasons, end),
                Shifts = shifts,
                ChartTitle = Loc.T("İŞGAL ORANI"),
                ChartRange = Loc.F("VARDİYA 1–{0}", shifts),
                OccupiedLabel = now > 0.05 ? Loc.F("İŞGAL · {0}", DeskFormat.Percent(now)) : Loc.T("İŞGAL"),
                RetakenLabel = Loc.T("GERİ ALINAN"),
                MomentsTitle = Loc.T("KIRILMA ANLARI"),
                Action = Loc.T("YENİDEN BAŞLA"),
            };
            switch (S.Ending)
            {
                case "topluluk":
                    d.Headline = Loc.T("Sığınakta görev yapabilecek kimse kalmadı.");
                    d.Sub = end.Text;
                    break;
                case "silme":
                    d.Headline = Loc.F("Bütün bölgeler özgür. YZ'nin son sunucusu {0}. vardiyada düştü.", shifts);
                    d.Sub = Loc.T("AĞA BAĞLANMA · ANALOG KAL");
                    d.SubIsMotto = true;
                    break;
                case "dagilma":
                    d.Headline = Loc.T("Sivil güven sıfıra indi; Sığınak 07 dağıldı.");
                    d.Sub = end.Text;
                    break;
                case "vuruldu":
                    d.Headline = Loc.F("Komutan kapıda vuruldu; Sığınak 07 {0}. vardiyada düştü.", shifts);
                    d.Sub = end.Text;
                    break;
                case "dusus":
                    d.Headline = Loc.F("Konum çözüldü; Sığınak 07 {0}. vardiyada düştü.", shifts);
                    d.Sub = end.Text;
                    break;
                case "terk":
                    d.Headline = Loc.F("Sığınak 07 {0}. vardiyada boşaldı: kalanlar onu ayakta tutamadı.", shifts);
                    d.Sub = end.Text;
                    break;
                case "salter":
                    d.Headline = evac
                        ? Loc.F("Son şalter {0}. vardiyada indi; Sığınak 07 tahliye edildi.", shifts)
                        : Loc.F("Son şalter {0}. vardiyada indi; Sığınak 07 düzenli çekildi.", shifts);
                    d.Sub = evac ? EvacuationText(reasons) : end.Text;
                    break;
                default:
                    d.Headline = Loc.F("Sığınak 07'nin yeri {0}. vardiyada YZ'ye sızdı.", shifts);
                    d.Sub = end.Text;
                    break;
            }

            d.Indicators.Add(new FinalIndicator { Label = Loc.T("SÜRE"), Value = shifts.ToString(), Sub = Loc.T("vardiya") });
            d.Indicators.Add(new FinalIndicator { Label = Loc.T("BASKIN"), Value = raids.ToString(), Sub = Loc.F("{0} başarılı · {1} başarısız", wins, raids - wins) });
            d.Indicators.Add(new FinalIndicator
            {
                Label = Loc.T("ÖZGÜR BÖLGE"), Value = Loc.F("{0}/{1}", free, regions.Count), Sub = Loc.F("{0}/{1} sektör geri alındı", captured, sectors),
                Tone = free == regions.Count ? OperationTone.Retaken : OperationTone.Plain,
            });
            int population = sim.Board != null ? sim.Board.People.Count(p => p.Alive) : S.Liberation.Shelter.Population;
            d.Indicators.Add(new FinalIndicator
            {
                Label = Loc.T("SIĞINAK NÜFUSU"), Value = population.ToString(),
                Sub = won ? Loc.T("kişi") : salter ? Loc.T("kişi çıktı") : Loc.T("kişi kaldı"),
                Tone = outcome == FinalOutcome.Lost ? OperationTone.Alert : OperationTone.Plain,
            });

            // Şerit: harekât tam işgalle başlar; kayıt vardiya sonunda (gün = biten vardiya), bugünkü oranla biter
            d.Points.Add(new Vector2(1, 100));
            foreach (var h in S.Hist)
                if (h.Day >= 1 && h.Day < shifts && h.Day > d.Points[d.Points.Count - 1].x) d.Points.Add(new Vector2(h.Day, (float)h.Real));
            d.Points.Add(new Vector2(shifts, (float)now));

            var moments = Moments(sim, won, evac, reasons);
            for (int i = 0; i < moments.Count; i++)
            {
                var m = moments[i];
                d.Moments.Add(new FinalMoment
                {
                    Number = i + 1, Shift = Mathf.Clamp(m.Day + 1, 1, shifts), Code = Code(m.Day), Text = m.Text,
                    Alert = m.Final && outcome == FinalOutcome.Lost,
                });
            }

            if (r != null)
            {
                d.ToneLine = ToneLine(sim, r, evac, reasons);
                Criteria(sim, r, d, evac, reasons);
                d.Record = Record(sim, r);
                d.MainTab = Loc.T("HAREKÂT");
                d.RecordTab = Loc.T("SİCİL");
            }
            return d;
        }

        // ---------------------------------------------------------------- damga ve metin (KOS aşama 6)

        static string ReasonStamp(string ending, EndReport r, bool evac, List<string> reasons, EndingDef end)
        {
            switch (ending)
            {
                case "silme":
                    return r?.Tone switch
                    {
                        "temiz" => Loc.T("TEMİZ"),
                        "bedelli" => Loc.T("BEDELLİ"),
                        "golgeli" => Loc.T("GÖLGELİ"),
                        _ => null,
                    };
                case "dusus": return Loc.T("SIĞINAK DÜŞTÜ");
                case "terk": return Loc.T("SIĞINAK BOŞALDI");
                case "salter":
                    if (!evac) return Loc.T("ÇEKİLME");
                    return LastSwitchViewBuilder.ReasonStamp(reasons.Count > 0 ? reasons[0] : null);
                default: return DeskText.Upper(end.Title);
            }
        }

        /// <summary>Tahliyenin ikinci satırı: "çekilme emri" değil, tahliye emri; sondaki yan cümle ilk nedene göre.</summary>
        static string EvacuationText(List<string> reasons)
        {
            string first = reasons.Count > 0 ? reasons[0] : "konum";
            return first switch
            {
                "kadro" => Loc.T("Şalter indi, tahliye emri damgalandı. Sığınak 07 karardı; eksik kalan kadro karanlığa yürüdü."),
                "aclik" => Loc.T("Şalter indi, tahliye emri damgalandı. Sığınak 07 karardı; aç kalanlar karanlığa yürüdü."),
                "guven" => Loc.T("Şalter indi, tahliye emri damgalandı. Sığınak 07 karardı; çıkanlar sırtını dönmüş bir halkın arasından karanlığa yürüdü."),
                _ => Loc.T("Şalter indi, tahliye emri damgalandı. Sığınak 07 karardı; çıkanlar konumu çözülmüş sığınaktan karanlığa yürüdü."),
            };
        }

        /// <summary>Ton satırı: sonun gerekçesi sayılarla (tonun keyfî görünmemesi için ölçüt kutusuyla birlikte).</summary>
        static string ToneLine(Simulation sim, EndReport r, bool evac, List<string> reasons)
        {
            switch (r.Ending)
            {
                case "silme":
                    if (r.Tone == "golgeli")
                    {
                        // Core'un golge ölçütüyle aynı küme: içeride, izi görmezden gelinmiş ortaya çıkmamış sızmacı
                        int minIgnored = Math.Max(1, sim.B.victoryShadowIgnoredTraces);
                        var listening = r.Hidden.Where(h => h.Where == "icerde" && h.IgnoredTraces >= minIgnored).Select(h => h.Name).ToList();
                        return Loc.F("Son sunucu düştü. İçeride {0} hâlâ dinliyordu.", Names(listening, ToneNamesMaxChars));
                    }
                    if (r.Tone == "temiz")
                    {
                        var founders = r.Survivors.Where(p => p.Founder).Select(p => p.Name).ToList();
                        return Loc.F("Temiz zafer: {0} kurucu sağ: {1}.", founders.Count, Names(founders, ToneNamesMaxChars));
                    }
                    var costs = new List<string>();
                    foreach (var c in r.Criteria)
                    {
                        if (c.Met) continue;
                        switch (c.Id)
                        {
                            case "olum": costs.Add(Loc.F("{0} kayıp", (int)c.Value)); break;
                            case "aclik": costs.Add(Loc.F("{0} ayrılan", (int)c.Value)); break;
                            case "ifsa": costs.Add(Loc.F("ifşa {0}", (int)c.Value)); break;
                            case "guven": costs.Add(Loc.F("en düşük güven {0}", (int)c.Value)); break;
                        }
                    }
                    return Loc.F("Bedelli zafer: {0}.", string.Join(" · ", costs));
                case "dusus":
                {
                    int traces = r.ExposureSteps.Count(x => x.Cause == "iz" || x.Cause == "ikinci_iz");
                    int sabotage = r.ExposureSteps.Count(x => x.Cause == "sabotaj");
                    return Loc.F("İfşa {0}/{1}: {2} görmezden gelinen iz, {3} sabotaj. İçeride {4} kişi kaldı · {5} bölge özgürdü.",
                        r.Exposure, sim.B.exposureThreshold, traces, sabotage, r.Survivors.Count, r.RegionsFree);
                }
                case "terk":
                    return Loc.F("Açlıktan ayrılan {0} · kalan {1} · sığınak eksik {2}/{3}.", r.Deserted, r.Survivors.Count,
                        sim.Shelter.UnderstaffedStreak, sim.B.abandonUnderstaffedShifts);
                case "salter":
                    return evac
                        ? Loc.F("Neden: {0}.", LastSwitchViewBuilder.ReasonList(sim, reasons))
                        : Loc.F("Kurtarılan {0} kişi · {1} özgür bölge bırakıldı.", r.Survivors.Count, r.RegionsFree);
                default:
                    return null;
            }
        }

        /// <summary>Ölçüt kutusu: zaferde ton ölçütleri, şalterde çeşit, düşüş ve terkte neden (değer, sınır, hüküm).</summary>
        static void Criteria(Simulation sim, EndReport r, FinalReportData d, bool evac, List<string> reasons)
        {
            FinalCriterionRow Row(string label, string value, string limit, bool? met)
                => new FinalCriterionRow { Label = label, Value = value, Limit = limit, Met = met, Verdict = met == null ? null : met.Value ? Loc.T("TUTTU") : Loc.T("TUTMADI") };

            switch (r.Ending)
            {
                case "silme":
                    d.CriteriaTitle = Loc.T("TON ÖLÇÜTLERİ");
                    foreach (var c in r.Criteria)
                    {
                        string v = DeskFormat.Int((int)c.Value), l = DeskFormat.Int((int)c.Limit);
                        switch (c.Id)
                        {
                            case "golge": d.Criteria.Add(Row(Loc.T("Gölge: izi görmezden gelinen sızmacı içeride"), v, Loc.F("en çok {0}", l), c.Met)); break;
                            case "olum": d.Criteria.Add(Row(Loc.T("Ölüm"), v, Loc.F("en çok {0}", l), c.Met)); break;
                            case "aclik": d.Criteria.Add(Row(Loc.T("Açlıktan ayrılan"), v, Loc.F("en çok {0}", l), c.Met)); break;
                            case "ifsa": d.Criteria.Add(Row(Loc.T("İfşa"), v, Loc.F("en çok {0}", l), c.Met)); break;
                            case "guven": d.Criteria.Add(Row(Loc.T("En düşük güven"), v, Loc.F("en az {0}", l), c.Met)); break;
                        }
                    }
                    d.CriteriaRule = Loc.T("İlki tutmazsa gölgeli; hepsi tutarsa temiz; değilse bedelli.");
                    break;
                case "salter":
                {
                    d.CriteriaTitle = Loc.T("ÇEŞİT");
                    FinalCriterionRow Reason(string id, string label, string value, string limit)
                    {
                        bool hit = reasons.Contains(id);
                        return new FinalCriterionRow { Label = label, Value = value, Limit = limit, Alert = hit, Verdict = hit ? Loc.T("TAHLİYE NEDENİ") : null };
                    }
                    // Evet/hayır ve işliyor/işlemiyor Core'un çekimde yazdığı nedenlerden (SwitchReasons); sayılar son anın durumundan (son
                    // komutundan sonra komut yok; bağ KosFinalReportTests.Switch_LiveReadersAgreeWithRecordedReasons'ta kilitli)
                    d.Criteria.Add(Reason("konum", Loc.T("Konum çözülmüş müydü"), reasons.Contains("konum") ? Loc.T("evet") : Loc.T("hayır"), null));
                    d.Criteria.Add(Reason("kadro", Loc.T("Sığınak eksik sayacı"),
                        reasons.Contains("kadro") ? Loc.F("işliyor {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts) : Loc.T("işlemiyor"), null));
                    d.Criteria.Add(Reason("aclik", Loc.T("Açlıktan ayrılma"),
                        reasons.Contains("aclik") ? Loc.F("sürüyor · aç {0}/{1}", sim.Shelter.HungryStreak, sim.B.desertStreak) : Loc.T("yok"), null));
                    d.Criteria.Add(Reason("guven", Loc.T("Güven"), DeskFormat.Int(sim.S.Trust), Loc.F("tahliye sınırı < {0}", sim.B.evacuationTrustBelow)));
                    d.CriteriaRule = Loc.T("Bunlardan biri tutarsa tahliye, yenilgi sayılır; hiçbiri tutmazsa düzenli çekilme.");
                    break;
                }
                case "dusus":
                {
                    d.CriteriaTitle = Loc.T("NEDEN");
                    var warn = sim.ShelterEvents?.Log.LastOrDefault(x => x.Kind == "konum");
                    int asked = warn == null ? 0 : r.ExposureSteps.Count(x => x.Cause == "sorgula" && x.Shift >= warn.Shift);
                    d.Criteria.Add(Row(Loc.T("İfşa"), Loc.F("{0}/{1}", r.Exposure, sim.B.exposureThreshold), null, null));
                    d.Criteria.Add(Row(Loc.T("Konum çözüldü"), warn != null ? Code(warn.Shift) : "—", null, null));
                    d.Criteria.Add(Row(Loc.T("Uyarı süresinde sorgu"), asked == 0 ? Loc.T("yok") : DeskFormat.Int(asked), null, null));
                    break;
                }
                case "terk":
                    d.CriteriaTitle = Loc.T("NEDEN");
                    d.Criteria.Add(Row(Loc.T("Açlıktan ayrılan"), DeskFormat.Int(r.Deserted), null, null));
                    d.Criteria.Add(Row(Loc.T("Kalan"), DeskFormat.Int(r.Survivors.Count), Loc.F("acil eşik < {0}", sim.B.candidateEmergencyBelow), null));
                    d.Criteria.Add(Row(Loc.T("Sığınak eksik"), Loc.F("{0}/{1}", sim.Shelter.UnderstaffedStreak, sim.B.abandonUnderstaffedShifts), null, null));
                    break;
            }
        }

        /// <summary>SİCİL: SIĞINAK (kalanlar, kayıplar, ayrılanlar), KAPI (sayımlar), İFŞA ve ORTAYA ÇIKMAMIŞ (adımlar, gizliler).</summary>
        static List<FinalSection> Record(Simulation sim, EndReport r)
        {
            var list = new List<FinalSection>();

            // SIĞINAK
            string keptLabel = r.Ending switch
            {
                "salter" => Loc.T("Kurtarılanlar"),
                "dusus" => Loc.T("İçeride kalanlar"),
                _ => Loc.T("Kalanlar"),
            };
            var shelter = new List<string>
            {
                Loc.F("{0} ({1}): {2}", keptLabel, r.Survivors.Count, Names(r.Survivors.Select(p => p.Stretcher ? Loc.F("{0} (sedyeyle)", p.Name) : p.Name).ToList(), RecordNamesMaxChars)),
                Loc.F("Kayıplar ({0}): {1}", r.Dead.Count, Names(r.Dead.Select(p => p.Name).ToList(), RecordNamesMaxChars)),
                Loc.F("Ayrılanlar ({0}): {1}", r.Deserters.Count, Names(r.Deserters.Select(p => p.Where == "aclik"
                    ? Loc.F("{0} ({1}, açlık)", p.Name, Code(p.Shift)) : Loc.F("{0} (bırakıldı)", p.Name)).ToList(), RecordNamesMaxChars)),
            };
            bool alert = false;
            if (r.Ending == "salter")
            {
                var carried = r.Survivors.Where(p => p.Infiltrator).Select(p => p.Name).ToList();
                if (carried.Count > 0)
                {
                    shelter.Add(Loc.F("Kurtarılanlar arasında ortaya çıkmamış sızmacı vardı: {0}.", Names(carried, ToneNamesMaxChars)));
                    alert = true;
                }
            }
            list.Add(new FinalSection { Title = Loc.T("SIĞINAK"), Body = string.Join("\n", shelter), Alert = alert });

            // KAPI
            var door = new List<string>
            {
                Loc.F("Alınan aday: {0}", r.Admitted),
                r.SentHumans > 0
                    ? Loc.F("Yollanan: {0} ({1} insan: {2})", r.Sent, r.SentHumans, Names(r.SentHumanList.Select(p => p.Name).ToList(), DoorNamesMaxChars))
                    : Loc.F("Yollanan: {0}", r.Sent),
                Loc.F("Sökülen: {0}", r.Removed),
                Loc.F("Geri çevrilen: {0} ({1} sızmacı)", r.Passed, r.PassedInfiltrators),
            };
            list.Add(new FinalSection { Title = Loc.T("KAPI"), Body = string.Join("\n", door) });

            // İFŞA ve ORTAYA ÇIKMAMIŞ
            var lines = new List<string>();
            var steps = r.ExposureSteps.Select(x => Loc.F("{0} · {1} · {2} · {3}", Code(x.Shift), Cause(x.Cause), string.IsNullOrEmpty(x.Name) ? "—" : x.Name,
                DeskFormat.Signed(x.Delta, 0))).ToList();
            if (steps.Count == 0) lines.Add(Loc.T("İfşa adımı yok."));
            else if (steps.Count <= MaxExposureLines) lines.AddRange(steps);
            else
            {
                int max = MaxExposureLines, head = max / 2, tail = max - head - 1;
                lines.AddRange(steps.Take(head));
                lines.Add(Loc.F("… +{0}", steps.Count - head - tail));
                lines.AddRange(steps.Skip(steps.Count - tail));
            }
            lines.Add(Loc.F("= {0} / eşik {1}", r.Exposure, sim.B.exposureThreshold));
            lines.Add(string.Empty);
            if (r.Hidden.Count == 0) lines.Add(Loc.T("Ortaya çıkmamış sızmacı yok."));
            else
                foreach (var h in r.Hidden.Take(MaxHiddenLines))
                    lines.Add(h.Clues == 1
                        // Tekil biçim ayrı anahtar ("1 traces" olmasın)
                        ? Loc.F("{0} · kabul {1} · tek iz ({2} görmezden gelindi) · {3}", h.Name, Code(Math.Max(0, h.AcceptedShift)), h.IgnoredTraces, Where(h.Where))
                        : Loc.F("{0} · kabul {1} · {2} iz ({3} görmezden gelindi) · {4}", h.Name, Code(Math.Max(0, h.AcceptedShift)), h.Clues, h.IgnoredTraces, Where(h.Where)));
            if (r.Hidden.Count > MaxHiddenLines) lines.Add(Loc.F("… +{0}", r.Hidden.Count - MaxHiddenLines));
            list.Add(new FinalSection { Title = Loc.T("İFŞA VE ORTAYA ÇIKMAMIŞ"), Body = string.Join("\n", lines), Alert = r.Hidden.Count > 0 });
            return list;
        }

        /// <summary>İfşa kaynağının adı (SİCİL).</summary>
        public static string Cause(string id) => id switch
        {
            "iz" => Loc.T("görmezden gelinen iz"),
            "ikinci_iz" => Loc.T("görmezden gelinen ikinci iz"),
            "sabotaj" => Loc.T("sabotaj"),
            "sorgula" => Loc.T("sorgu"),
            _ => Loc.T("başka kaynak"),
        };

        static string Where(string id) => id switch
        {
            "icerde" => Loc.T("içeride"),
            "oldu" => Loc.T("öldü"),
            "yollandi" => Loc.T("yollandı"),
            "sokuldu" => Loc.T("söküldü"),
            "aclik" => Loc.T("açlıktan ayrıldı"),
            "birakildi" => Loc.T("bırakıldı"),
            _ => Loc.T("ayrıldı"),
        };

        /// <summary>
        /// Adların ", " ile birleşimi; <paramref name="maxChars"/>'ı aşarsa sığan adlar ve "… +n". Liste boşsa "—". En az bir ad yazılır.
        /// </summary>
        public static string Names(IReadOnlyList<string> names, int maxChars)
        {
            if (names == null || names.Count == 0) return "—";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                string next = i == 0 ? names[i] : ", " + names[i];
                int left = names.Count - i - 1;
                // kalan varsa "… +n" için yer bırak (kesimin gerçek uzunluğu, o anki dilde)
                int reserve = left > 0 ? Loc.F("{0} … +{1}", string.Empty, left).Length : 0;
                if (i > 0 && sb.Length + next.Length + reserve > maxChars)
                    return Loc.F("{0} … +{1}", sb.ToString(), names.Count - i);
                sb.Append(next);
            }
            return sb.ToString();
        }

        struct Candidate
        {
            public int Day, Order;
            public string Text;
            public bool Final;
        }

        /// <summary>Kırılma anları: zorunlular (ilk özgür bölge, kaybın nedeni, son olay), kalan yerlere karşı saldırılar, sonra öteki özgür bölgeler.</summary>
        static List<Candidate> Moments(Simulation sim, bool won, bool evac, List<string> reasons)
        {
            var S = sim.S;
            var freed = S.RaidLog.Where(l => l.Success && l.LiberationReward?.RegionCompleted == true).ToList();
            var must = new List<Candidate>();
            var counters = S.Moments.Where(m => m.Kind == "bad").Select(m => new Candidate { Day = m.Day, Order = 1, Text = Counterattack(sim, m) }).ToList();
            // Tesis aşama 11: kuşatmanın kurulma ve karar anları (kötü an) kırılma anı değildir; kanadın düşüşü "yeniden işgal" anıyla gelir
            if (FacilityDefenseText.On(sim))
                counters = S.Moments.Where(m => m.Kind == "bad").Select(m => (m, Text: Reoccupation(sim, m)))
                    .Where(x => x.Text != null).Select(x => new Candidate { Day = x.m.Day, Order = 1, Text = x.Text }).ToList();
            var others = new List<Candidate>();

            Candidate? final = null;
            if (S.Ending == "silme" && freed.Count > 0)
            {
                var last = freed[freed.Count - 1];
                final = new Candidate { Day = last.Day, Order = 3, Text = Loc.F("{0} özgür, son sunucu düştü", Name(sim, last.Region)), Final = true };
            }
            else if (S.Ending == "dagilma") final = new Candidate { Day = S.Day, Order = 3, Text = Loc.T("Güven 0 · sığınak dağıldı"), Final = true };
            else if (S.Ending == "dusus" || S.Ending == "terk" || S.Ending == "salter")
            {
                // KOS aşama 6: sığınağın sonları kendi son anlarını ve zorunlu ek anlarını yazar (günlük public)
                string text = S.Ending == "dusus" ? Loc.T("Konum çözüldü · Sığınak 07 düştü")
                    : S.Ending == "terk" ? Loc.T("Sığınak boşaldı")
                    : evac ? Loc.T("Son şalter · tahliye") : Loc.T("Son şalter · çekilme");
                final = new Candidate { Day = S.Day, Order = 3, Text = text, Final = true };
                var log = sim.ShelterEvents?.Log;
                if (log != null)
                {
                    if (S.Ending == "dusus" || evac && reasons.Contains("konum"))
                    {
                        var warn = log.LastOrDefault(x => x.Kind == "konum");
                        if (warn != null) must.Add(new Candidate { Day = warn.Shift, Order = 2, Text = Loc.T("KONUM ÇÖZÜLDÜ uyarısı") });
                    }
                    if (S.Ending == "terk")
                    {
                        var first = log.FirstOrDefault(x => x.Kind == "ayrilma");
                        if (first != null)
                            must.Add(new Candidate { Day = first.Shift, Order = 2, Text = Loc.F("İlk açlık ayrılması: {0}", ShelterEventSnapshotBuilder.NameOf(sim, first.PersonId)) });
                    }
                }
            }
            else
            {
                var fatal = S.Door.Log.LastOrDefault(v => v.Ending != null);
                if (fatal != null) final = new Candidate { Day = fatal.Day, Order = 3, Text = Loc.F("Kapı açıldı · {0}", Loc.T(DoorCards.Get(fatal.Card).Who)), Final = true };
                else final = new Candidate { Day = S.Day, Order = 3, Text = GameData.Ending(S.Ending).Title, Final = true };
            }

            for (int i = 0; i < freed.Count; i++)
            {
                if (won && i == freed.Count - 1) continue;
                var c = new Candidate { Day = freed[i].Day, Order = 0, Text = i == 0 ? Loc.F("{0} özgür, ilk bölge", Name(sim, freed[i].Region)) : Loc.F("{0} özgür", Name(sim, freed[i].Region)) };
                if (i == 0) must.Add(c); else others.Add(c);
            }
            if (S.Ending == "dagilma" && Cause(sim) is Candidate cause) must.Add(cause);
            must.Add(final.Value);

            int room = Math.Max(0, MaxMoments - must.Count);
            var picked = Spread(counters, room);
            picked.AddRange(Spread(others, room - picked.Count));
            return must.Concat(picked).OrderBy(c => c.Day).ThenBy(c => c.Order).ToList();
        }

        /// <summary>En çok <paramref name="n"/> an; fazlaysa ilk ve son korunur, aradakiler eşit aralıkla.</summary>
        static List<Candidate> Spread(List<Candidate> xs, int n)
        {
            if (n <= 0) return new List<Candidate>();
            if (xs.Count <= n) return xs.ToList();
            if (n == 1) return new List<Candidate> { xs[xs.Count - 1] };
            return Enumerable.Range(0, n).Select(i => xs[(int)Math.Round(i * (xs.Count - 1) / (double)(n - 1))]).ToList();
        }

        /// <summary>
        /// “YZ karşı saldırısı: Doğu Avrupa 01 yeniden işgal”. Sektör anın metninden bulunur (an, yazıldığı dilde durur: her dilin
        /// biçimi denenir); bulunamazsa metin olduğu gibi.
        /// </summary>
        static string Counterattack(Simulation sim, Moment m) => Reoccupation(sim, m) ?? Loc.F("YZ karşı saldırısı: {0}", m.Text.TrimEnd('.'));

        /// <summary>Anın metni bir sektörün yeniden işgaliyse "YZ karşı saldırısı: … yeniden işgal"; değilse null.</summary>
        static string Reoccupation(Simulation sim, Moment m)
        {
            foreach (var s in sim.S.Liberation.Regions.SelectMany(r => r.Sectors))
                foreach (var lang in Loc.Languages)
                {
                    // Anlar kayıttan yeniden oynatılınca bugünkü oda adıyla yazılır (sektör adı bölgeden)
                    var loc = DeskLoc.Instance;
                    string text = string.Format(loc.In(Loc.N("{0}: {1} yeniden işgal altında."), lang), loc.In(sim.RegionSourceName(s.Region), lang),
                        loc.In(GameData.Region(s.Region).SourceSector(s.Index), lang));
                    if (text == m.Text) return Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", Name(sim, s.Region), s.Index + 1);
                }
            return null;
        }

        /// <summary>Dağılma'nın nedeni: son güven düşüşü (kapıda verilen karar ya da sivil sunucu kıran baskın); karşı saldırıysa o zaten listede.</summary>
        static Candidate? Cause(Simulation sim)
        {
            var S = sim.S;
            var door = S.Door.Log.Where(v => v.Trust < 0).Select(v => (v.Day, Text: Door(v))).LastOrDefault();
            var raid = S.RaidLog.Where(l => l.LiberationReward != null && l.LiberationReward.Trust < 0 && l.Collateral > 0)
                .Select(l => (l.Day, Text: Loc.F("Baskında {0} sivil sunucu kırıldı · güven {1}", l.Collateral, DeskFormat.Signed(l.LiberationReward.Trust, 0)))).LastOrDefault();
            if (door.Text == null && raid.Text == null) return null;
            var last = raid.Text == null || door.Text != null && door.Day >= raid.Day ? door : raid;
            int counter = S.Moments.Where(m => m.Kind == "bad").Select(m => m.Day).DefaultIfEmpty(-1).Max();
            if (counter > last.Day) return null;
            return new Candidate { Day = last.Day, Order = 2, Text = last.Text };
        }

        static string Door(DoorVisit v)
        {
            var card = DoorCards.Exists(v.Card) ? DoorCards.Get(v.Card) : null;
            if (card == null) return Loc.F("Kapı · güven {0}", DeskFormat.Signed(v.Trust, 0));
            string label = DoorViewBuilder.ChoiceLabel(v, card, out _);
            return label == null
                ? Loc.F("Kapı: {0} · güven {1}", Loc.T(card.Who), DeskFormat.Signed(v.Trust, 0))
                : Loc.F("Kapı: {0} · “{1}” · güven {2}", Loc.T(card.Who), Loc.T(label), DeskFormat.Signed(v.Trust, 0));
        }
    }
}
