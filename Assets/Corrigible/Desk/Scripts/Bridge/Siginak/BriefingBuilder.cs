using System;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Kayıttaki harekâtın özeti (KAYITTAN DEVAM sekmesi): vardiya ve özgür bölge.</summary>
    public struct SavedSummary
    {
        public int Shift, Free, Regions;
    }

    /// <summary>
    /// Emir belgesinin metinleri (K7). Gövde taslaktır (K7 notu › metin): TR ≤ 520, EN ≤ 680 karakter, dört paragraf.
    /// Kayıt özeti kaydın bir kez yeniden oynatılmasından hesaplanır (Soru 4); kayıt dosyası değişmez.
    /// </summary>
    public static class BriefingBuilder
    {
        /// <summary>Kaydı yeniden oynatıp vardiya ve özgür bölge sayısını çıkarır; kayıt yoksa ya da açılamazsa null.</summary>
        public static SavedSummary? Summarize(GameRecord record)
        {
            if (record == null) return null;
            try
            {
                var sim = Replay.Restore(record);
                if (sim == null) return null;
                var lib = sim.S.Liberation;
                return new SavedSummary { Shift = sim.S.Day + 1, Free = lib.Regions.Count(r => r.Completed), Regions = lib.Regions.Count };
            }
            catch (Exception e)
            {
                Debug.LogWarning("Kayıt özeti çıkarılamadı: " + e.Message);
                return null;
            }
        }

        /// <param name="reading">H / EVRAK tepsisi: dosya ve sekme yok, düğme “Kapat”.</param>
        /// <param name="armed">Kayıt varken ilk Enter basıldı: düğme onay ister.</param>
        /// <param name="dossier">Manila dosya ve KAYITTAN DEVAM sekmesi (menüden yeni harekâtta yok).</param>
        /// <param name="sim">
        /// Açık harekât: sığınağın bölgesinin adı haritadan (<see cref="Simulation.RegionName"/>). Üretilmiş haritada
        /// (<see cref="Simulation.Map"/>) ilk iki paragraf diegetik gerekçeyi taşır: YZ uydu verisini ve haritaları sildi,
        /// vaziyet kâğıdı telsiz ve kablo raporlarından çizilir. Harekât yoksa dünya haritasının metni.
        /// </param>
        public static BriefingData Build(bool reading, SavedSummary? save, bool armed, bool dossier = true, Simulation sim = null)
        {
            string hq = sim != null ? sim.RegionName(Simulation.HQ) : GameData.Region(Simulation.HQ).Ai;
            bool drawn = sim?.Map != null;
            return new BriefingData
            {
                Agency = Loc.T("KYGA · KÜRESEL YAPAY ZEKÂ GÜVENLİK AJANSI"),
                Number = Loc.F("EMİR NO. {0:000}", 1),
                Title = Loc.T("HAREKÂT EMRİ"),
                ToLabel = Loc.T("KİME"),
                To = Loc.F("SIĞINAK 07 KOMUTANI · {0}", DeskText.Upper(hq)),
                ReLabel = Loc.T("KONU"),
                Re = Loc.T("KARŞI HAREKÂT"),
                Stamp = Loc.T("GİZLİ"),
                // “ALINDI” baskın ekranında ganimet anlamında çevrili (COLLECTED); belge damgası vardiyayla ayrı anahtar
                Received = Loc.F("ALINDI · V.{0:000}", 1),
                Paragraphs = new[]
                {
                    drawn
                        ? Loc.T("Dünya çoktan düştü. Yapay zekâ on iki bölgeyi, bütün ağları ve sunucuları ele geçirdi; uydu verisini ve haritaları sildi.")
                        : Loc.T("Dünya çoktan düştü. Yapay zekâ on iki bölgenin tamamını, bütün ağları ve sunucuları ele geçirdi. Yukarıda konuşan her makine artık onun sesiyle konuşuyor."),
                    drawn
                        ? Loc.F("SIĞINAK 07, {0} bölgesinin altında, hâlâ bizim. Ağ yok: emirler kâğıtla verilir, vaziyet kâğıdı telsiz ve kablo raporlarından çizilir.", hq)
                        : Loc.F("SIĞINAK 07, {0} bölgesinin altında, hâlâ bizim. Ağ yok: emirler kâğıtla verilir, sahayla yalnız telsiz konuşur.", hq),
                    Loc.T("Görevin: Alfa ekibini sahaya sür, bölgeleri sektör sektör geri al, sığınağı ayakta tut. Sivilleri koru: kırılan her sunucu GÜVEN'i düşürür, sıfırda direniş dağılır. Zaman yalnızca sen VARDİYAYI BİTİR dediğinde ilerler."),
                },
                Closing = Loc.T("AĞA BAĞLANMA. ANALOG KAL."),
                SignOffice = Loc.T("KYGA MERKEZ KOMUTANLIĞI"),
                SignLine = Loc.F("Vardiya {0:000} · analog hat", 1),
                Action = reading ? Loc.T("KAPAT") : armed ? Loc.T("BAŞLA · ONAY") : Loc.T("BAŞLA"),
                Reading = reading,
                NoDossier = !dossier,
                HasSave = save != null,
                TabTitle = Loc.T("KAYITTAN DEVAM"),
                TabSummary = save is SavedSummary s ? Loc.F("V.{0:000} · özgür {1}/{2}", s.Shift, s.Free, s.Regions) : "",
                TabHint = Loc.T("dosyayı aç"),
                NoSave = Loc.T("KAYIT YOK"),
            };
        }

        /// <summary>Kartlı harekâtın emri aynı belge düzeninde; geçici raf/yedek dili kullanılmaz.</summary>
        public static BriefingData Planning(Simulation sim, bool reading, SavedSummary? save, bool armed, bool dossier)
        {
            var data = Build(reading, save, armed, dossier, sim);
            data.Paragraphs[2] = Loc.F("Emrindeki {0} kişi bu sığınağın topluluğudur. Saha kadrosunu panoda kur, bölgeleri sektör sektör geri al. Kaybettiğin kişi geri gelmez. Sivilleri koru: GÜVEN sıfırda direniş dağılır. Vardiya, devir fişi EVRAK'a konunca biter.", sim.B.boardStartResidents);
            return data;
        }

        /// <summary>Kayıt varken ilk BAŞLA'nın teleks uyarısı (K7 notu › durumlar; teleks şeridine sığacak kadar kısa).</summary>
        public static (string status, string message) Warning(SavedSummary save) =>
            (Loc.T("KAYIT VAR"), Loc.F("V.{0:000} kaydının üstüne yazılır · yeniden bas", save.Shift));
    }
}
