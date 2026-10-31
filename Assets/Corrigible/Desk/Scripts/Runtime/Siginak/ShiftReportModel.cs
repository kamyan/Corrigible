using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    // ---------- Girdi: iki anlık görüntü (kayda girmez, liberation_desk.json'da son 5 rapor) ----------

    [Serializable]
    public sealed class ShiftRoomSnapshot
    {
        public string id;
        public int level, workers;
    }

    [Serializable]
    public sealed class ShiftMemberSnapshot
    {
        /// <summary>Rol (tek, emp, ana); yara (eksik can) ve yatacağı gün.</summary>
        public string role;
        public int wounds, down;
    }

    [Serializable]
    public sealed class ShiftTeamSnapshot
    {
        public string id, name;
        /// <summary>Ekip bu vardiya baskına çıktı (dinlenmedi).</summary>
        public bool busy;
        public List<ShiftMemberSnapshot> members = new List<ShiftMemberSnapshot>();
        /// <summary>Kadroda Teknisyen var (asıl ya da yedek): baskına çıkabilir.</summary>
        public bool canRaid = true;

        public ShiftMemberSnapshot Member(string role)
        {
            foreach (var m in members) if (m.role == role) return m;
            return null;
        }
    }

    /// <summary>Vardiyadaki tek fon hareketi (simülasyonun işlem defterinden): tür, hedef, tutar ve sunum için bir sayı.</summary>
    [Serializable]
    public sealed class ShiftLedgerEntry
    {
        public string kind, id;
        public double fon;
        /// <summary>Baskın: sektör numarası (OP-02 → 2). Oda: bu komuttan sonraki seviye (1 = onarım).</summary>
        public int number;
    }

    /// <summary>Raporun açıklama satırlarında kullanılan denge değerleri (Balance'tan, raporla birlikte saklanır).</summary>
    [Serializable]
    public sealed class ShiftRates
    {
        public double dailyFunds, regionIncome;
        public double suppliesPerWorker, suppliesPerResident, starvingMultiplier;
        public double platesPerWorker, plateMaterials, healPerWorker;
        public int healingDays, healingWorkers, workersPerLevel, restHeal;
        public int counterattackTrust;
    }

    /// <summary>Vardiyanın bir anı: kol inmeden önce (kapanış öncesi) ya da sonra (yeni vardiyanın başı).</summary>
    [Serializable]
    public sealed class ShiftSnapshot
    {
        public int shift, freeRegions, regions, population, idle;
        public double fon, supplies, suppliesCapacity, materials, materialsCapacity;
        public bool starving;
        /// <summary>Zırh plakaları, deposu ve sıradaki plakanın birikimi; hazır yedek uzman.</summary>
        public int plates, plateCapacity, reserves;
        public double plateProgress;
        /// <summary>YZ alarmı ve eşiği; karşı saldırı sayısı ve sonuncusunda yeniden işgal edilen sektör.</summary>
        public double alarm, alarmThreshold;
        public int counterattacks;
        public string lastCounterattack;
        /// <summary>Biten kapı ziyareti sayısı (vardiya sonunda açılmadı sayılanları ayırmak için).</summary>
        public int doorVisits;
        public List<ShiftRoomSnapshot> rooms = new List<ShiftRoomSnapshot>();
        public List<ShiftTeamSnapshot> teams = new List<ShiftTeamSnapshot>();

        public ShiftRoomSnapshot Room(string id)
        {
            foreach (var r in rooms) if (r.id == id) return r;
            return null;
        }

        public ShiftTeamSnapshot Team(string id)
        {
            foreach (var t in teams) if (t.id == id) return t;
            return null;
        }
    }

    /// <summary>Vardiya biterken kapıda kalan, açılmadı sayılan ziyaretçi: kart kimliği ve gerçekleşen etki.</summary>
    [Serializable]
    public sealed class ShiftDoorEntry
    {
        public string card, result;
        public double supplies, materials, alarm;
        public int trust;
        public bool counterattack;
    }

    /// <summary>
    /// Raporun bütün girdisi: kapanış öncesi ve sonrası anlık görüntü, vardiyanın fon hareketleri, adımın geliri,
    /// teleksteki YAPILAMADI satırları ve teleks kodu. Metin taşımaz (yalnız teleks satırları): dil değişince rapor
    /// yeniden kurulur. Değerler simülasyondan; açıklama satırlarındaki formüller <see cref="rates"/>'ten.
    /// </summary>
    [Serializable]
    public sealed class ShiftReportInput
    {
        public string code;
        public ShiftSnapshot before = new ShiftSnapshot(), after = new ShiftSnapshot();
        public List<ShiftLedgerEntry> ledger = new List<ShiftLedgerEntry>();
        /// <summary>Adımın geliri (merkez + özgür bölge payı).</summary>
        public double incomeCentral, incomeRegional;
        public List<TelexLine> failures = new List<TelexLine>();
        public ShiftRates rates = new ShiftRates();
        /// <summary>Vardiya biterken açılmadı sayılan kapı ziyaretleri.</summary>
        public List<ShiftDoorEntry> door = new List<ShiftDoorEntry>();
        /// <summary>Bu vardiyada YZ karşı saldırdıysa teleksteki “YZ KARŞI SALDIRISI” satırının kodu (raporun kutusunda).</summary>
        public string counterCode;
    }

    // ---------- Görünüm verisi ----------

    /// <summary>
    /// Değerin anlamı (Revizyon 2 · S5): “+” yeşil, “−” mürekkep (eksi işaretiyle), “±0” gri; düz değer mürekkep; uyarı
    /// kırmızı (yalnız alarm eşiği ve işgal). İşaret rengi tek başına taşımaz; renk anahtarı <see cref="ReportTones"/>'ta.
    /// </summary>
    public enum ReportTone { Plain, Good, Bad, Neutral, Alert }

    /// <summary>Renk kuralı (rapor, özet gözleri ve ± fişleri aynı kuralı kullanır): tonun tema renk anahtarı.</summary>
    public static class ReportTones
    {
        public static string ColorKey(ReportTone tone) => tone switch
        {
            ReportTone.Good => "report.positive",
            ReportTone.Bad => "report.negative",
            ReportTone.Neutral => "report.neutral",
            ReportTone.Alert => "report.counterAttackLine",
            _ => "ink",
        };
    }

    public sealed class ReportRow
    {
        /// <summary>Satır adı (LF 400 18) ve yanındaki açıklama (LF 400 15, ör. “2 + 0,5 × 1 özgür bölge”).</summary>
        public string Label, Note;
        /// <summary>Satır içi damga (ör. “DEPO DOLU”); yoksa null.</summary>
        public string Badge;
        public string Value;
        public ReportTone Tone;
        /// <summary>Toplam satırı: üstte 1 px çizgi, kalın ad, CP 700 22.</summary>
        public bool Total;
    }

    /// <summary>Ekip satırı: ad · durum, sağda damga (“HAZIR”, “yara 3 → 1”), altında dinlenme, revir ve yatan uzmanlar.</summary>
    public sealed class ReportTeamRow
    {
        public string Label, Value, Detail;
        /// <summary>Damganın rengi: hazır yeşil, öteki mürekkep.</summary>
        public ReportTone Tone;
        /// <summary>Ekip simgesi gösterilmez (ekip olmayan satır: kapı).</summary>
        public bool HideIcon;
    }

    /// <summary>
    /// Özet kutusunun bir gözü (Revizyon 2 · S5): başlık + işaretli değişim, büyük sayı (CP 32, sınır 20 px), altında satır.
    /// </summary>
    public sealed class ReportEye
    {
        public string Label, Delta, Value, Line;
        /// <summary>Üzerine gelince etiket (alarm gözünde “duvarda: YZ ALARMI”); yoksa null.</summary>
        public string Hover;
        public ReportTone DeltaTone;
        /// <summary>Büyük sayı ve satır kırmızı (alarm eşikte).</summary>
        public bool Alert;
    }

    /// <summary>
    /// Vardiya raporunun gösterime hazır hâli (Revizyon 2 · S5): <see cref="ShiftReportView"/> yalnız bunu çizer. Başta
    /// (karşı saldırıda) çift çizgili kutu, sonra üç göz (ERZAK · YZ ALARMI · FON) ve alarm notu; dökümde erzak ve alarm bir
    /// daha yazılmaz.
    /// </summary>
    public sealed class ShiftReportData
    {
        public string Head, Code, Title, Sub, Stamp;
        /// <summary>Karşı saldırı kutusu: başlık, teleks kodu, sektör satırı; karşı saldırı yoksa başlık null.</summary>
        public string CounterTitle, CounterCode, CounterText;
        public ReportEye Supplies = new ReportEye(), Alarm = new ReportEye(), FundsEye = new ReportEye();
        /// <summary>Alarm ölçeği: dolu hücre, hücre sayısı (eşik) ve kırmızı bölgenin ilk hücresi (1'den).</summary>
        public int AlarmCells, AlarmSteps, AlarmHighFrom;
        /// <summary>Özetin altındaki alarm notu (karşı saldırıda yok).</summary>
        public string AlarmNote;
        public string FundsTitle, ProductionTitle, TeamTitle, FailedTitle;
        public readonly List<ReportRow> Funds = new List<ReportRow>();
        public readonly List<ReportRow> Production = new List<ReportRow>();
        /// <summary>Analist notu (Kalam, en çok bir satır, yalnız öneri); yoksa null.</summary>
        public string Analyst;
        public readonly List<ReportTeamRow> Teams = new List<ReportTeamRow>();
        /// <summary>YAPILAMADI satırları: kod (CP 700) + ileti.</summary>
        public readonly List<(string code, string message)> Failures = new List<(string, string)>();
        public string MoreFailures;
        public string Action, Hint;

        /// <summary>Sayaç fişleri (VARDİYA, FON, ERZAK, MALZEME sırasıyla): metin + anlam.</summary>
        public readonly (string text, ReportTone tone)[] Slips = new (string, ReportTone)[4];

        // Testler ve dizi için sayılar
        public double Opening, Closing, Income, SuppliesNet, MaterialsNet;
        public bool TeamReturned;
        /// <summary>Bu vardiyada YZ karşı saldırdıysa teleks iletisi; yoksa null.</summary>
        public string Counterattack;
    }
}
