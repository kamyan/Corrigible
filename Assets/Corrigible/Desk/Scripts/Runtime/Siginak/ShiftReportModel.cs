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
    public sealed class ShiftTeamSnapshot
    {
        public string id, name;
        public int down;
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
        public double suppliesPerWorker, suppliesPerResident, materialsPerWorker, starvingMultiplier, workerEnergy;
        public int healingDays, healingWorkers, workersPerLevel;
    }

    /// <summary>Vardiyanın bir anı: kol inmeden önce (kapanış öncesi) ya da sonra (yeni vardiyanın başı).</summary>
    [Serializable]
    public sealed class ShiftSnapshot
    {
        public int shift, freeRegions, regions, population, idle;
        public double fon, supplies, suppliesCapacity, materials, materialsCapacity, energyCapacity, energyDemand;
        public bool starving;
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

        public bool EnergyEnough => energyCapacity >= energyDemand;
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
    }

    // ---------- Görünüm verisi ----------

    /// <summary>Değerin anlamı: “+” yeşil, “−” kırmızı, “±0” gri; düz değer mürekkep. İşaret rengi tek başına taşımaz.</summary>
    public enum ReportTone { Plain, Good, Bad, Neutral }

    public sealed class ReportRow
    {
        /// <summary>Satır adı (LF 400 18) ve yanındaki açıklama (LF 400 15, ör. “2 + 0,5 × 1 özgür bölge”).</summary>
        public string Label, Note;
        /// <summary>Satır içi damga (ör. “ENERJİSİZ 4/6”); yoksa null.</summary>
        public string Badge;
        public string Value;
        public ReportTone Tone;
        /// <summary>Toplam satırı: üstte 1 px çizgi, kalın ad, CP 700 22.</summary>
        public bool Total;
    }

    /// <summary>Ekip toparlanması satırı: ad · durum, “2 → 1 gün”, altında revir ve hazır olma cümlesi.</summary>
    public sealed class ReportTeamRow
    {
        public string Label, Value, Detail;
    }

    /// <summary>Vardiya raporunun gösterime hazır hâli (K6b): <see cref="ShiftReportView"/> yalnız bunu çizer.</summary>
    public sealed class ShiftReportData
    {
        public string Head, Code, Title, Sub, Stamp;
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
    }
}
