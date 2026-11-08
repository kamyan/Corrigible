using System;
using System.Collections.Generic;
using Corrigible.Core;

namespace Corrigible.Desk
{
    /// <summary>
    /// <see cref="HandoverSlipFacts"/> → o anki dilde fiş satırları (Revizyon 3 · R19c, PLAN W3 ve §4). Bütün sözcükler
    /// <c>Loc.T</c> / <c>Loc.F</c> dizgi sabitleriyle; sayılar <see cref="DeskFormat"/> ile (TR virgül, EN nokta, eksi U+2212),
    /// büyük harf <see cref="DeskText.Upper(string)"/> ile. Kişi adları çevrilmez. Saf: yalnız verilen veriyi okur, durum tutmaz.
    /// <list type="bullet">
    /// <item>Saha satırı: disk harfi ustalık harflerinin ilk üçü (<c>Loc.N("T E A Ü M B İ")</c>, pano kartıyla aynı anahtar), ad
    /// (boş cep "—"), eşyalar " · " ile (kit · zırh · EMP hücresi · bakım kiti).</item>
    /// <item>Sığınak satırı: ad, görev (ÜRETİM, MUTFAK, EĞİTİM · KONU, BOŞTA) ve durum eki (yaralı, yatıyor n, kısıtlı n,
    /// kısıtlı · bitti). Görev kalın yazılır, ek ince (yüz birleştirir).</item>
    /// <item>KAPANIŞTA BEKLENEN: ERZAK "önce → sonra / tavan" ve kalın fark; EĞİTİM (kademe değişirse kalın "→ EĞİTİMLİ",
    /// değişmezse "(2→4)"); ÜRETİM; KAPI.</item>
    /// <item>Uyarılar (Karar D9-A): tür başına tek kutu, aynı türün adları virgülle (veride tek tek ya da virgülle birleşik
    /// gelebilir; ad ad ayrılır), sıra verideki ilk görünüş sırası (köprü Core'un sırasıyla yazar: kapı, kısıt süresi doldu,
    /// iki bulgu, inceleme). Yüz sığmazsa son kutuyu <see cref="WarningText"/> ile "… +n" diye kısaltır.</item>
    /// </list>
    /// </summary>
    public static class HandoverSlipFormat
    {
        /// <summary>Eşya listesi ve görev ile durum eki arasındaki ayraç (sözcük değil; dile göre değişmez).</summary>
        public const string Separator = " · ";
        /// <summary>Aynı türden uyarının adları arasındaki ayraç.</summary>
        public const string NameSeparator = ", ";
        /// <summary>Boş saha cebi.</summary>
        public const string EmptyPocket = "—";

        /// <summary>Fişin satırları; <paramref name="f"/> null ise boş satırlar.</summary>
        public static HandoverSlipLines Format(HandoverSlipFacts f)
        {
            var l = new HandoverSlipLines();
            if (f == null) return l;
            int shift = Math.Max(0, f.shift), copy = Math.Max(1, f.copy);

            l.HeaderLeft = Loc.T("KYGA · SIĞINAK 07");
            l.HeaderRight = Loc.F("No. {0:000}/{1}", shift, copy);
            l.Title = Loc.F("VARDİYA {0:000} · DEVİR FİŞİ", shift);
            l.Subtitle = Loc.F("{0}/{1} kişi · erzak {2}/{3}", f.alive, f.capacity, Floor(f.supplies), Floor(f.suppliesCapacity));

            l.FieldLabel = Loc.T("SAHA · BASKIN KADROSU");
            var letters = MasteryLetters();
            if (f.field != null)
                foreach (var r in f.field)
                {
                    if (r == null) continue;
                    l.Field.Add(new HandoverSlipLines.Row
                    {
                        Disc = DiscLetter(r.role, letters),
                        Name = string.IsNullOrEmpty(r.name) ? EmptyPocket : r.name,
                        Value = string.IsNullOrEmpty(r.name) ? string.Empty : Items(r.items),
                    });
                }

            l.ShelterLabel = Loc.T("SIĞINAK");
            if (f.shelter != null)
                foreach (var r in f.shelter)
                {
                    if (r == null) continue;
                    l.Shelter.Add(new HandoverSlipLines.Row
                    {
                        Name = r.name ?? string.Empty,
                        Value = JobWord(r.job, r.subject),
                        Suffix = StateWord(r.state, r.stateShifts),
                    });
                }

            l.ExpectedLabel = Loc.T("KAPANIŞTA BEKLENEN");
            l.Expected.Add(Rations(f));
            l.Expected.Add(Training(f.training));
            l.Expected.Add(new HandoverSlipLines.Row { Name = Loc.T("ÜRETİM"), Value = Production(f.production) });
            l.Expected.Add(new HandoverSlipLines.Row
            {
                Name = Loc.T("KAPI"),
                Value = f.openApplications > 0 ? Loc.F("{0} açık başvuru", f.openApplications) : Loc.T("açık başvuru yok"),
            });

            if (f.warnings != null)
                foreach (var w in f.warnings)
                {
                    if (w == null) continue;
                    var box = l.Warnings.Find(b => b.Kind == w.kind);
                    if (box == null)
                    {
                        box = new HandoverSlipLines.Box { Kind = w.kind, Prefix = WarningPrefix(w.kind) };
                        l.Warnings.Add(box);
                    }
                    // köprü aynı türün adlarını virgülle birleştirip yazar: kesim (… +n) için ad ad ayrılır
                    if (string.IsNullOrEmpty(w.name)) continue;
                    foreach (var part in w.name.Split(new[] { NameSeparator }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string name = part.Trim();
                        if (name.Length > 0 && !box.Names.Contains(name)) box.Names.Add(name);
                    }
                }
            foreach (var box in l.Warnings) box.Text = WarningText(box.Kind, box.Names);

            l.Footer = Loc.F("plan {0:000}/{1} · tek kullanımlık · yeniden basılmaz", shift, copy);
            l.Action = Loc.T("fişi EVRAK’a koy");
            return l;
        }

        // ---------------------------------------------------------------- uyarılar

        /// <summary>Uyarı kutusunun öneki: "KAPI ·" ya da "DOSYA ·".</summary>
        public static string WarningPrefix(SlipWarningKind kind) => kind == SlipWarningKind.Door ? Loc.T("KAPI ·") : Loc.T("DOSYA ·");

        /// <summary>
        /// Uyarı kutusunun metni. <paramref name="shown"/>: kutuda yazılan ad sayısı; adların hepsi sığmıyorsa kalanlar
        /// "… +n" olur (D9-A: punto küçülmez, kutu kesilir). Kapı uyarısı ad taşımaz.
        /// </summary>
        public static string WarningText(SlipWarningKind kind, IReadOnlyList<string> names, int shown = int.MaxValue)
        {
            if (kind == SlipWarningKind.Door) return Loc.T("Açık kapı başvurusu kararsızsa bu kapanışta reddedilir.");
            string who = Names(names, shown);
            switch (kind)
            {
                case SlipWarningKind.FileRestrictionExpired: return Loc.F("{0} · kısıtlı süre doldu", who);
                case SlipWarningKind.FileClues: return Loc.F("{0} · iki bağımsız bulgu", who);
                default: return Loc.F("{0} · inceleme fişi açık", who);
            }
        }

        static string Names(IReadOnlyList<string> names, int shown)
        {
            int count = names?.Count ?? 0;
            if (count == 0) return EmptyPocket;
            shown = Math.Max(1, Math.Min(shown, count));
            if (shown >= count) return string.Join(NameSeparator, names);
            var head = new string[shown];
            for (int i = 0; i < shown; i++) head[i] = names[i];
            return Loc.F("{0} … +{1}", string.Join(NameSeparator, head), count - shown);
        }

        // ---------------------------------------------------------------- satırlar

        static HandoverSlipLines.Row Rations(HandoverSlipFacts f)
        {
            // Fark ekranda yazılan iki değerden: "24,0 → 21,9" yanında "−2,1" (yuvarlama farkı görünmesin)
            double before = Math.Round(f.supplies, 1, MidpointRounding.AwayFromZero);
            double after = Math.Round(f.suppliesAfter, 1, MidpointRounding.AwayFromZero);
            return new HandoverSlipLines.Row
            {
                Name = Loc.T("ERZAK"),
                Value = Loc.F("{0} → {1} / {2}", DeskFormat.Number(before, 1), DeskFormat.Number(after, 1), DeskFormat.Int(Floor(f.suppliesCapacity))),
                Suffix = DeskFormat.Signed(after - before, 1),
            };
        }

        static HandoverSlipLines.Row Training(SlipTraining t)
        {
            var row = new HandoverSlipLines.Row { Name = Loc.T("EĞİTİM") };
            if (t == null || !t.active || string.IsNullOrEmpty(t.name))
            {
                row.Value = Loc.T("eğitim yok");
                return row;
            }
            string topic = TopicLabel(t.subject);
            int gain = t.xpAfter - t.xpBefore;
            if (t.tierAfter != t.tierBefore)
            {
                row.Strong = DeskText.Upper(TierWord(t.tierAfter));
                row.Value = Loc.F("{0} · {1} +{2} → {3}", t.name, topic, gain, row.Strong);
            }
            else row.Value = Loc.F("{0} · {1} +{2} ({3}→{4})", t.name, topic, gain, t.xpBefore, t.xpAfter);
            return row;
        }

        static string Production(SlipProduction p)
        {
            if (p == null || p.kind == SlipProductionKind.None || string.IsNullOrEmpty(p.itemType)) return Loc.T("iş emri yok · çıktı yok");
            string item = PlanningBoardData.EquipmentName(p.itemType);
            if (p.kind == SlipProductionKind.NoOutput) return Loc.F("{0} · çıktı yok", item);
            return Loc.F("{0} {1}/{2} → {3}/{2} · {4}", item, p.done, p.turns, p.after, string.IsNullOrEmpty(p.maker) ? EmptyPocket : p.maker);
        }

        /// <summary>Saha eşyasının fişteki kısa adı (küçük harf): kit, zırh, EMP hücresi, bakım kiti.</summary>
        public static string ItemWord(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return string.Empty;
            if (kind.StartsWith("kit_", StringComparison.Ordinal)) return Loc.T("kit");
            switch (kind)
            {
                case "armor": return Loc.T("zırh");
                case "medkit": return Loc.T("bakım kiti");
                case "emp_cell": return Loc.T("EMP hücresi");
                default: return PlanningBoardData.EquipmentName(kind);
            }
        }

        static string Items(List<string> items)
        {
            if (items == null || items.Count == 0) return string.Empty;
            var words = new List<string>(items.Count);
            foreach (var kind in items)
            {
                string w = ItemWord(kind);
                if (!string.IsNullOrEmpty(w)) words.Add(w);
            }
            return string.Join(Separator, words);
        }

        /// <summary>Sığınak görevinin büyük harfli adı: ÜRETİM, MUTFAK, EĞİTİM · KONU, öteki her şey BOŞTA.</summary>
        public static string JobWord(string job, string subject)
        {
            switch (job)
            {
                case "craft": return Loc.T("ÜRETİM");
                case "cook": return Loc.T("MUTFAK");
                case "train":
                    return string.IsNullOrEmpty(subject) ? Loc.T("EĞİTİM") : Loc.F("EĞİTİM · {0}", DeskText.Upper(TopicLabel(subject)));
                default: return DeskText.Upper(Loc.T("Boşta"));
            }
        }

        /// <summary>Durum eki (ince): yaralı, yatıyor n, kısıtlı n, kısıtlı · bitti; durum yoksa boş.</summary>
        public static string StateWord(SlipPersonState state, int shifts)
        {
            switch (state)
            {
                case SlipPersonState.Hurt: return Loc.T("yaralı");
                case SlipPersonState.Down: return Loc.F("yatıyor {0}", shifts);
                case SlipPersonState.Restricted: return Loc.F("kısıtlı {0}", shifts);
                case SlipPersonState.RestrictionEnded: return Loc.T("kısıtlı · bitti");
                default: return string.Empty;
            }
        }

        /// <summary>Eğitim konusunun kısa adı (karışık harf; pano kartındaki <c>TopicLabel</c> ile aynı anahtarlar).</summary>
        public static string TopicLabel(string subject)
        {
            switch (subject)
            {
                case "tek": return Loc.T("Teknisyen");
                case "emp": return Loc.T("EMP");
                case "ana": return Loc.T("Analiz");
                case "craft": return Loc.T("Üretim");
                case "cook": return Loc.T("Mutfak");
                case "care": return Loc.T("Bakım");
                case "gather": return Loc.T("İkmal");
                default: return PlanningBoardData.SubjectName(subject);
            }
        }

        /// <summary>Kademe sözcüğü (küçük harf; pano kişi dosyasıyla aynı anahtarlar).</summary>
        public static string TierWord(int tier)
        {
            switch (tier)
            {
                case 3: return Loc.T("usta");
                case 2: return Loc.T("deneyimli");
                case 1: return Loc.T("eğitimli");
                default: return Loc.T("acemi");
            }
        }

        /// <summary>Yedi ustalık harfi (TR T E A Ü M B İ · EN T E A P K C F), tek çeviri anahtarından.</summary>
        static string[] MasteryLetters()
        {
            string source = Loc.N("T E A Ü M B İ");
            var parts = Loc.T(source).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == PlanningBoardData.Subjects.Length ? parts : source.Split(' ');
        }

        static string DiscLetter(string role, string[] letters)
        {
            int i = Array.IndexOf(PlanningBoardData.Subjects, role);
            return i >= 0 && i < letters.Length ? letters[i] : string.Empty;
        }

        static int Floor(double v) => (int)Math.Floor(v + 1e-9);
    }

    /// <summary>
    /// Fişin o anki dildeki satırları, yukarıdan aşağı yüzdeki sırasıyla: başlık satırı (sol/sağ), başlık, alt başlık, SAHA,
    /// SIĞINAK ve KAPANIŞTA BEKLENEN bölümleri, uyarı kutuları, alt satır ve eylem satırı. Düz metin (zengin metin etiketi
    /// yok); kalın kısımları yüz ekler.
    /// </summary>
    public sealed class HandoverSlipLines
    {
        /// <summary>
        /// Bir satır: disk harfi (yalnız saha), ad ya da etiket, sağdaki değer, ek (sığınakta durum eki, ERZAK'ta fark).
        /// <see cref="Strong"/>: değerin sonundaki kalın sözcük (EĞİTİM'in "→ EĞİTİMLİ"si); yoksa boş.
        /// </summary>
        public sealed class Row
        {
            public string Disc = string.Empty, Name = string.Empty, Value = string.Empty, Suffix = string.Empty, Strong = string.Empty;
        }

        /// <summary>Uyarı kutusu: tür, önek (“KAPI ·”), metin ve (DOSYA'da) kutudaki adlar.</summary>
        public sealed class Box
        {
            public SlipWarningKind Kind;
            public string Prefix = string.Empty, Text = string.Empty;
            public readonly List<string> Names = new List<string>();
        }

        public string HeaderLeft = string.Empty, HeaderRight = string.Empty, Title = string.Empty, Subtitle = string.Empty;
        public string FieldLabel = string.Empty, ShelterLabel = string.Empty, ExpectedLabel = string.Empty;
        public readonly List<Row> Field = new List<Row>();
        public readonly List<Row> Shelter = new List<Row>();
        public readonly List<Row> Expected = new List<Row>();
        public readonly List<Box> Warnings = new List<Box>();
        public string Footer = string.Empty, Action = string.Empty;
    }
}
