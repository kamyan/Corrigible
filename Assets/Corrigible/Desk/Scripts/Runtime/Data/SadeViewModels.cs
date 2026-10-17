using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    // Sade arayüzün görünüm modeli (Docs/UI_Sade › spec/mock/masa_*.json). Değerler gösterilecek değerlerdir; arayüz hesap
    // yapmaz, yalnız DeskFormat ile biçimlendirir. Etiketler Türkçe kaynak metindir (DeskLoc çevirir).

    /// <summary>Üst köşedeki kâğıt etiket: FON, SİYASİ GÜÇ, KÜRESEL GÜVEN, BAĞIMLILIK, TEKİLLİK.</summary>
    [Serializable]
    public sealed class SadeChipVM
    {
        public string Id, Label, Icon;
        public float Value;
        public int Decimals;
        /// <summary>verified, report, suspicious, unknown.</summary>
        public string Trust;
        public float Delta;
        public bool DeltaGood, Approx, Percent, ThreatIcon;
        /// <summary>Değerin yanındaki küçük “±6” (rapor belirsizliği); 0 = yok.</summary>
        public float Uncertainty;
        public SadeBarVM Bar;
        /// <summary>Üstüne gelince döküm fişi (S-Masa3); null = yok.</summary>
        public string Tooltip;
    }

    /// <summary>Eşik çubuğu (yüzde 0–100). Boş dizi = o parça yok.</summary>
    [Serializable]
    public sealed class SadeBarVM
    {
        public float Width;
        public float[] Hazard = Array.Empty<float>();
        public float Marker = -1;
        public float Fill = -1;
        public float[] Uncertainty = Array.Empty<float>();
        /// <summary>Dolgu rengi tema anahtarı (ör. “sade.desk.bar.fill.singularity”).</summary>
        public string FillKey;
    }

    /// <summary>HEDEF satırı: Hizalama ya da Silme.</summary>
    [Serializable]
    public sealed class SadeGoalVM
    {
        public string Id, Label;
        public float Percent;
        /// <summary>good (Hizalama, yeşil) ya da info (Silme, mavi).</summary>
        public string Color;
    }

    // Zaman plakası C’nin ClockVM’ini kullanır (gün, saat, hız, saatin yerel ilerleme hızı).

    [Serializable]
    public sealed class SadeDockVM
    {
        public List<SadeDockTabVM> Tabs = new List<SadeDockTabVM>();
        /// <summary>Açık çekmecenin kimliği (research, news, teams, aiLog, guide); null = kapalı.</summary>
        public string Open;
    }

    [Serializable]
    public sealed class SadeDockTabVM
    {
        public string Id, Label, Key;
        /// <summary>count (sarı sayı) ya da dot (kırmızı nokta); null = rozet yok.</summary>
        public string BadgeType;
        public int BadgeValue;
    }
}
