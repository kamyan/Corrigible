using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Önlem defteri (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3; aşama 10): YZ'nin saydığı altı araç sınıfı, sınıf başına önlemin adı,
    /// kademelerin etkisi, sınıfın cevap tarifi ve öğrenmenin saf seçimi. Tablolar burada statiktir, sayılar <see cref="Balance"/>'tadır.
    /// Tahtadaki kurallar <c>Baskin.Counters.cs</c>'te, harekât durumu <see cref="FacilityState"/>'te. Durum yazmaz, RNG çekmez.
    /// </summary>
    public static class FacilityCounters
    {
        // ---------- Sınıflar ----------
        /// <summary>Sınıf kimlikleri (durum anahtarı; kayıt dizgisi değil).</summary>
        public const string Push = "itme", Breach = "yikim", Build = "orme", Capture = "ele", Dog = "kopek", Gadget = "aygit";

        /// <summary>Tablo sırası: eşitlikte belirleyici; sayaç ve kademe listelerinin (<c>Tally</c>, <c>Counter</c>) dizini bu sıradır.</summary>
        public static readonly string[] Classes = { Push, Breach, Build, Capture, Dog, Gadget };

        /// <summary>Sınıfın tablo sırası; bilinmeyen −1.</summary>
        public static int Index(string cls) => cls == null ? -1 : Array.IndexOf(Classes, cls);

        static readonly string[] ClassNames =
            { Loc.N("İtme"), Loc.N("Yıkım"), Loc.N("Örme"), Loc.N("Ele geçirme"), Loc.N("Köpek"), Loc.N("Aygıt") };

        static readonly string[] CounterNames =
            { Loc.N("Cıvatalı ayak"), Loc.N("Çift cidar"), Loc.N("Kesici kol"), Loc.N("Kısa bağ"), Loc.N("Koku yayıcı"), Loc.N("Parazit") };

        /// <summary>Sınıfın gösterim adı (geçerli dilde); bilinmeyende boş.</summary>
        public static string ClassName(string cls)
        {
            int i = Index(cls);
            return i < 0 ? "" : Loc.T(ClassNames[i]);
        }

        /// <summary>Önlemin adı (geçerli dilde): aygıt sınıfında 2. kademe "Makine aklı", öbür kademeler ve sınıflar tek ad.</summary>
        public static string Name(string cls, int level)
        {
            int i = Index(cls);
            if (i < 0) return "";
            return cls == Gadget && level >= 2 ? Loc.T("Makine aklı") : Loc.T(CounterNames[i]);
        }

        /// <summary>Tahtada robotun üstündeki kısa etiket (en çok 9 harf): yalnız cıvatalı ayak (öbür önlemler bütün makinelere işler).</summary>
        public static string Short(string cls) => cls == Push ? Loc.T("CIVATALI") : null;

        /// <summary>Kademenin tek satırlık etkisi (geçerli dilde; sayılar dengeden). Bilinmeyen sınıf ya da kademe 0: boş.</summary>
        public static string Desc(Balance b, string cls, int level)
        {
            b = b ?? new Balance();
            if (level <= 0) return "";
            bool two = level >= 2;
            switch (cls)
            {
                case Push: return two ? Loc.T("Bakım robotu da itilmez ve çekilmez.") : Loc.T("İmza makineleri itilmez ve çekilmez.");
                case Breach:
                    return two ? Loc.F("Hedef odada da {0} duvar {1} hak ister.", b.facilityDoubleWalls, b.facilityDoubleWallCharges)
                        : Loc.F("Giriş ve ara odada {0} duvar {1} hak ister.", b.facilityDoubleWalls, b.facilityDoubleWallCharges);
                case Build:
                    return two ? Loc.F("Makineler kalas duvara da {0} fazla hasar verir.", b.facilityCutterDamage)
                        : Loc.F("Makineler tel örgüye {0} fazla hasar verir.", b.facilityCutterDamage);
                case Capture:
                    return two ? Loc.T("Süresi biten tutsak hemen niyet alır.") : Loc.F("Kelepçe {0} tur kısa sürer.", b.facilityShortClampTurns);
                case Dog: return Loc.F("Her odada {0} koku yayıcı: {1} kare içinde köpek komut alamaz.", Math.Min(2, level), b.facilityEmitterRange);
                case Gadget: return two ? Loc.T("Makineler pusu ve taret hattından kaçınır.") : Loc.F("Aygıtlar {0} tur kısa çalışır.", b.facilityJamTurns);
                default: return "";
            }
        }

        // ---------- Cevap tarifleri ----------
        /// <summary>Sınıfın cevap tarifi (<see cref="FacilityGear.Answers"/>); köpek sınıfında null (cevabı yayıcıyı kırmak).</summary>
        public static string AnswerOf(string cls)
        {
            switch (cls)
            {
                case Push: return FacilityGear.AnsSokum;
                case Breach: return FacilityGear.AnsPiercer;
                case Build: return FacilityGear.AnsSteelWire;
                case Capture: return FacilityGear.AnsRelay;
                case Gadget: return FacilityGear.AnsSpring;
                default: return null;
            }
        }

        /// <summary>Cevap tarifinin sınıfı (<see cref="AnswerOf"/>'un tersi); bilinmeyende null.</summary>
        public static string ClassOfAnswer(string answer) => answer == null ? null : Classes.FirstOrDefault(c => AnswerOf(c) == answer);

        // ---------- Tahta ----------
        /// <summary>İmza makinesi (cıvatalı ayağın 1. kademesi): dron ve bakım robotu dışındaki her makine türü.</summary>
        public static bool SignatureMachine(string type) => type != null && type != "dron" && type != "bakim";

        /// <summary>
        /// Bir baskının sayımı (saf): raporların sınıf başına sayaçlarının toplamı, <paramref name="cap"/> ile kesilir (en az 0). Kademe
        /// 2'deki sınıf tahtada zaten sayılmaz. Sonuç altı öğedir (<see cref="Classes"/> sırası).
        /// </summary>
        public static int[] RaidTally(IEnumerable<RoomReport> reports, int cap)
        {
            var o = new int[Classes.Length];
            foreach (var r in reports ?? Enumerable.Empty<RoomReport>())
                for (int i = 0; i < o.Length; i++) o[i] += r?.TallyAt(i) ?? 0;
            for (int i = 0; i < o.Length; i++) o[i] = Math.Min(Math.Max(0, cap), o[i]);
            return o;
        }

        // ---------- Öğrenme ----------
        /// <summary>
        /// Öğrenilecek önlemin sınıfı (saf; §6.3 "eşiği geçen en büyük sayaç"): aday kademesi 2'den küçük ve sayacı en az
        /// max(1, <paramref name="threshold"/>) olan sınıf; en büyük sayaç; eşitlikte <paramref name="log"/>'da son girdisi en eski
        /// (hiç girdisi olmayan en eski sayılır), sonra tablo sırası. Aday yoksa −1. Eksik liste öğesi 0 okunur.
        /// </summary>
        public static int Learn(IList<int> tally, IList<int> level, IList<FacilityCounterEntry> log, int threshold)
        {
            int need = Math.Max(1, threshold);
            int At(IList<int> l, int i) => l != null && i < l.Count ? l[i] : 0;
            int LastSeen(int i)
            {
                if (log == null) return -1;
                for (int k = log.Count - 1; k >= 0; k--) if (log[k]?.Class == Classes[i]) return k;
                return -1;
            }
            int best = -1;
            for (int i = 0; i < Classes.Length; i++)
            {
                if (At(level, i) >= 2 || At(tally, i) < need) continue;
                if (best < 0 || At(tally, i) > At(tally, best) || At(tally, i) == At(tally, best) && LastSeen(i) < LastSeen(best)) best = i;
            }
            return best;
        }
    }
}
