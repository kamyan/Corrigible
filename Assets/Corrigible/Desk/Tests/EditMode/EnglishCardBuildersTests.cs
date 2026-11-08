using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 10 (PLAN §1.2c): <c>LocalizationTests.EnglishGame_ProducesNoTurkishText</c>'in eksik yarısı. O test yalnız eski
    /// yolu oynar; bu test kartlı harekâtı gerçek çeviri tablosuyla İngilizce oynar ve Bridge kurucularının ürettiği her dizgeyi yansımayla
    /// toplar. Kurucuların hepsi saf okumadır (komut yazmaz, RNG çekmez); test Editor'ün çalışma durumuna, sahneye ya da prefab'a bakmaz.
    /// <para>
    /// Dil simülasyondan <b>önce</b> kurulur: Core'un gün 0 metinleri (haberler, ekip adları) üretildikleri dilde kalır, oyun sırasında dil
    /// değiştirmek bu testi yalancı kırardı. Kimlik alanları (<see cref="IdFields"/>, <c>…Id</c>, <c>…Key</c>, <c>…Color</c>) sayılmaz;
    /// kişi adları Türkçe harf taşıyabilir (İpek), o yüzden denetimden önce metinden çıkarılır, geri kalanı yine denetlenir.
    /// </para>
    /// Üç denetim birlikte çalışır: Türkçe harf (<see cref="TurkishLetters"/>), harfsiz Türkçe sözcük (<see cref="TurkishWords"/>: KABUL, SEVK)
    /// ve tablo anahtarı olan bir kaynak metnin çevrilmeden görünmesi. Ayrıca koşu boyunca <see cref="Loc.Missing"/> hiç tetiklenmemeli.
    /// </summary>
    public class EnglishCardBuildersTests
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const uint Seed = 79;

        /// <summary>Denetlenen vardiyalar (1 tabanlı, plan: V.001 / V.005 / V.009 / V.031); ulaşılamayan son vardiyalar atlanır.</summary>
        static readonly int[] LaterShifts = { 5, 9, 31 };

        /// <summary>Kapıda karar sırası: her kabul damgası en az bir kez iz bıraksın (KABUL, KISITLI, RED, YARDIM).</summary>
        static readonly string[] Decisions = { "admit", "restricted", "reject", "aid" };

        static readonly string[] Inspections = { "papers", "interview", "breath", "record" };

        // Büyük/küçük harf duyarlı: IgnoreCase "ı"yı "I" ile eşleştirir
        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]");

        /// <summary>
        /// Türkçe harf taşımayan sık Türkçe sözcükler: <c>LocalizationTests</c>'in listesi ve kart oyununun büyük harfli plaka/damga sözcükleri
        /// (İngilizcede de sözcük olan ya da kısa olup yanlış eşleşecek olanlar bilerek yok).
        /// </summary>
        static readonly Regex TurkishWords = new Regex(
            @"\b(bir|gun|fon|ekip|ekibi|bolge|sabotaj|elektrik|denetim|karantina|baskin|hasar|tur|yolda|hazir|ile|daha|yok|var|kez|sira|hedef|yetenek|bekliyor|ayakta|izleme" +
            @"|kabul|sevk|vardiya|erzak|teknisyen|uzman|malzeme|tahliye|sunucu|yedek|komutan|parola|plaka|belge|aday|sivil|yeniden|sonra|sonraki|kaynak|saha|sahada|sakin|cephe" +
            @"|fazla|yetersiz|ikmal|gerekiyor|gerekli|telsiz|devir|kapi|kadro|serbest|rapor|raporu|revir|dolu|iptal|operasyon|kilit|kilitli|doldu|bitti|gece|veya|hammadde" +
            @"|kayit|seviye|girdi|depo|deposu|kiti|uygun|kapat|eski|bulgu|eksik|yarali|gibi|alindi|inceleme|dosya|tarif|usta|kisitli|onay|zaten|kimse|edildi|kalan)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Görünmeyen kimlik alanları (ad kuralına uymayanlar); <see cref="HandoverSlipFacts"/>'in küçük harfli alanları da kimliktir.</summary>
        static readonly HashSet<string> IdFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "Kind", "Icon", "Trait", "Signature", "Subjects", "PocketOccupant", "DeckOrder", "role", "job", "subject", "items", "itemType",
        };

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        [Test]
        public void EnglishCardBuilders_ProduceNoTurkishText()
        {
            var english = Loc.ParseTable(File.ReadAllText(TablePath))[Loc.English];
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            var harvest = new Harvest();
            var reached = new List<int>();
            bool candidateSeen;
            try
            {
                Loc.SetLanguage(Loc.English, english);   // önce dil, sonra oyun
                candidateSeen = PlayMainLine(harvest, reached);
                PlayRetreat(harvest);
                harvest.Add("sim yok", BoardDropRules.CanPick(BoardCardKind.Person, "sakin-1", null));
            }
            finally { Loc.Missing -= OnMissing; }

            // Çift çeviri görünmez bir kusur: Core'un zaten çevrilmiş verdiği ad (GameData.Region(id).Name) yeniden Loc.T'ye girince olay tetiklenir
            missing.ExceptWith(english.Values);
            Assert.IsEmpty(missing, "Tabloda olmayan metinler:\n" + string.Join("\n", missing.Take(40)));
            var turkish = harvest.Turkish(english);
            Assert.IsEmpty(turkish, "Türkçe kalan metinler:\n" + string.Join("\n", turkish.Take(40)));

            CollectionAssert.IsSubsetOf(new[] { 1, 5 }, reached, "V.001 ve V.005 tohum 79'da ulaşılabilir olmalı.");
            Assert.IsTrue(candidateSeen, "Kapıda aday görülmedi: kabul kurucusu sınanmadı.");
            Assert.Greater(harvest.Count, 500, "Toplanan metin beklenenden az: yansıma veriyi görmüyor olabilir.");
        }

        // ================================================================== senaryolar (yalnız gerçek Core komutları)

        /// <summary>Kartlı harekât, tohum 79; açılış komutları panonun ilk açılışıyla aynı (<c>PlanningBoardPresenter.RetireJobs</c>).</summary>
        static Simulation NewGame()
        {
            var sim = Simulation.NewGame(Seed, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0 });
            Ok(sim.EnableAutomaticProduction());
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Ok(sim.AssignResident(p.Id, "rest"));
            return sim;
        }

        /// <summary>V.001'in iş emri, baskın ve kilitli plan durumları, sonra sıradaki vardiyalar. Kapıda aday görüldüyse true.</summary>
        static bool PlayMainLine(Harvest h, List<int> reached)
        {
            var sim = NewGame();
            Sweep(h, sim, "V.001");
            reached.Add(sim.S.Day + 1);

            Ok(sim.QueueProduction("armor"));
            Ok(sim.ReserveProductionMaterial("metal", sim.B.boardArmorMetal));
            Ok(sim.ReserveProductionMaterial("kumas", sim.B.boardArmorCloth));
            Sweep(h, sim, "V.001 iş emri");

            Ok(sim.StartLiberationRaid("weu_1"));
            Sweep(h, sim, "V.001 baskında");
            Ok(sim.ResolveRaid(new RaidResult { Success = true, Turns = 6 }));
            Sweep(h, sim, "V.001 dönüş");

            SendHome(sim);
            Ok(sim.PrintShift());
            Sweep(h, sim, "V.001 kilitli");
            Ok(sim.FileShift(sim.Board.ReceiptId));

            bool candidateSeen = false;
            int decision = 0;
            foreach (int shift in LaterShifts)
            {
                SendHome(sim);
                AdmissionFixtures.CloseTo(sim, shift - 1);
                if (sim.S.Day != shift - 1) break;
                reached.Add(shift);
                string tag = "V." + shift.ToString("000");
                Sweep(h, sim, tag);
                if (sim.Admission?.Candidate == null) continue;

                candidateSeen = true;
                foreach (string kind in Inspections) sim.InspectApplicant(kind);   // dış kayıt gözetmensiz reddedilebilir; ileti de toplanır
                Sweep(h, sim, tag + " incelendi");
                sim.DecideApplicant(Decisions[decision++ % Decisions.Length]);
                Sweep(h, sim, tag + " karar");
            }
            Endings(h, sim);
            return candidateSeen;
        }

        /// <summary>Geri çekilme: biri ölü, biri yaralı, biri geride; KAYIPLAR, yatan kart ve kısa vardiya sonrası.</summary>
        static void PlayRetreat(Harvest h)
        {
            var sim = NewGame();
            var squad = sim.PlanningSquad();
            Ok(sim.StartLiberationRaid("weu_1"));
            Ok(sim.ResolveRaid(new RaidResult { Retreat = true, Hp = { 0, squad[1].Hp, 0 }, Armor = { 0, 1, 0 }, Left = { 2 } }));
            Sweep(h, sim, "geri çekilme");
            SendHome(sim);
            AdmissionFixtures.CloseTo(sim, sim.S.Day + 1);
            Sweep(h, sim, "geri çekilme sonrası");
        }

        /// <summary>Yatan ya da kısıtlı saha görevlisini Boşta'ya alır: plan basılabilsin.</summary>
        static void SendHome(Simulation sim)
        {
            foreach (var p in sim.Board.People.Where(p => p.Alive && p.Job != "rest" && (p.Down > 0 || p.Restricted)).ToArray())
                sim.AssignResident(p.Id, "rest");
        }

        static void Ok(ActResult r) => Assert.IsTrue(r.Ok, r.Msg);

        // ================================================================== kurucular

        static void Sweep(Harvest h, Simulation sim, string tag)
        {
            h.LearnNames(sim);
            Board(h, sim, tag);
            Admission(h, sim, tag);
            Handover(h, sim, tag);
            Map(h, sim, tag);
            Papers(h, sim, tag);
            Telex(h, sim, tag);
            Drag(h, sim, tag);
        }

        static void Board(Harvest h, Simulation sim, string tag)
        {
            var snap = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), true, false, false);
            h.Walk(tag + "/Pano", snap);
            foreach (var person in snap.People)
            {
                h.Walk(tag + "/Pano seçili", PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = person.Id }, true, false, false).Folder);
                h.Walk(tag + "/Pano seçili, kilitli girdi", PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = person.Id }, true, true, true).Folder);
            }
            for (int row = 0; row < snap.Topics.Labels.Length; row++)
                h.Add(tag + "/Konu " + row, PlanningBoardSnapshotBuilder.TopicHoverLine(snap.Topics, row));
            h.Add(tag + "/Kilit", PlanningBoardSnapshotBuilder.LockReason(sim));
        }

        static void Admission(Harvest h, Simulation sim, string tag)
        {
            var ui = new AdmissionUi();
            var snap = AdmissionSnapshotBuilder.Build(sim, ui);
            h.Walk(tag + "/Kabul", snap);
            ui.DocumentOnCounterFor = sim.Admission?.Candidate?.Resident?.Id;
            var counter = AdmissionSnapshotBuilder.Build(sim, ui);
            h.Walk(tag + "/Kabul tezgâh", counter);

            foreach (AdmissionTool tool in Enum.GetValues(typeof(AdmissionTool)))
            {
                h.Add(tag + "/Araç", AdmissionSnapshotBuilder.ToolTitle(tool));
                h.Add(tag + "/Fiş", AdmissionSnapshotBuilder.SlipTitle(tool));
            }
            foreach (StampChoice choice in Enum.GetValues(typeof(StampChoice)))
            {
                h.Add(tag + "/Damga plakası", AdmissionSnapshotBuilder.StampPlate(choice));
                h.Add(tag + "/Damga sapı", AdmissionSnapshotBuilder.StampCap(choice));
                h.Walk(tag + "/Damga izi", AdmissionSnapshotBuilder.ImpressionLines(choice, sim.B));
            }
            h.Add(tag + "/Talep uyarısı", AdmissionSnapshotBuilder.RequestWarning(sim));
            foreach (var person in sim.Board.People.Where(p => p.Alive))
                h.Add(tag + "/İnceleme uyarısı", AdmissionSnapshotBuilder.ReviewWarning(sim, person.Id));
        }

        static void Handover(Harvest h, Simulation sim, string tag)
        {
            var facts = HandoverSlipFactsBuilder.Build(sim);
            h.Walk(tag + "/Fiş verisi", facts);
            // Veri dilden bağımsız; oyuncunun gördüğü sözcükler biçimleyiciden gelir
            h.Walk(tag + "/Fiş", HandoverSlipFormat.Format(facts));
            foreach (SlipWarningKind kind in Enum.GetValues(typeof(SlipWarningKind)))
            {
                h.Add(tag + "/Fiş uyarı öneki", HandoverSlipFormat.WarningPrefix(kind));
                h.Add(tag + "/Fiş uyarı", HandoverSlipFormat.WarningText(kind, new[] { "X" }));
            }
            foreach (SlipPersonState state in Enum.GetValues(typeof(SlipPersonState)))
                h.Add(tag + "/Fiş durum", HandoverSlipFormat.StateWord(state, 2));
        }

        static IEnumerable<string> RegionIds(Simulation sim) => sim.S.Liberation.Regions.Select(r => r.Id);

        static void Map(Harvest h, Simulation sim, string tag)
        {
            h.Walk(tag + "/Harita", LiberationMapBuilder.Build(sim));
            foreach (string id in RegionIds(sim))
            {
                h.Walk(tag + "/Harita seçili", LiberationMapBuilder.Build(sim, id, id));
                h.Add(tag + "/Harita kimlik", LiberationMapBuilder.Identity(id, true));
                h.Add(tag + "/Harita kimlik kısa", LiberationMapBuilder.Identity(id, false));
                var fields = new List<SlipField>();
                LiberationMapBuilder.Fields(sim, id, fields);
                h.Walk(tag + "/Harita alanlar", fields);
                h.Walk(tag + "/Görev fişi", LiberationMapBuilder.Slip(sim, id));
                h.Walk(tag + "/Kadro raporu", LiberationMapBuilder.SquadReport(sim, id));
                h.Walk(tag + "/Cephe fişi", LiberationMapBuilder.Front(sim, id));
                h.Add(tag + "/CRT kısa bölge", CrtViewBuilder.ShortRegion(id));
                h.Add(tag + "/Telsiz kısa bölge", RadioLogBuilder.Short(id));
            }
        }

        static void Papers(Harvest h, Simulation sim, string tag)
        {
            h.Walk(tag + "/Rapor", OperationReportBuilder.Build(sim));
            foreach (var log in sim.S.RaidLog) h.Walk(tag + "/Rapor kaydı", OperationReportBuilder.Build(sim, log));
            var save = new SavedSummary { Shift = 12, Free = 1, Regions = 12 };
            foreach (bool reading in new[] { false, true })
            foreach (bool armed in new[] { false, true })
            {
                h.Walk(tag + "/Emir", BriefingBuilder.Planning(sim, reading, null, armed, true));
                h.Walk(tag + "/Emir kayıtlı", BriefingBuilder.Planning(sim, reading, save, armed, false));
            }
            var warning = BriefingBuilder.Warning(save);
            h.Add(tag + "/Emir uyarı", warning.status);
            h.Add(tag + "/Emir uyarı", warning.message);
            foreach (bool raidLoaded in new[] { false, true })
            {
                h.Walk(tag + "/CRT", CrtViewBuilder.Screen(sim, raidLoaded));
                h.Walk(tag + "/Telsiz", RadioLogBuilder.Entries(sim, raidLoaded));
            }
            h.Walk(tag + "/CRT geçiş", CrtViewBuilder.Transition(sim));
        }

        static void Telex(Harvest h, Simulation sim, string tag)
        {
            foreach (var room in sim.Shelter.Rooms)
            {
                h.Add(tag + "/Teleks geliştirme", TelexReasons.Upgrade(sim, room.Id));
                for (int workers = 0; workers <= 3; workers++) h.Add(tag + "/Teleks personel", TelexReasons.Assign(sim, room.Id, workers));
            }
            foreach (string id in RegionIds(sim).Concat(new[] { null, "yok" }))
            {
                h.Add(tag + "/Teleks bırakma", TelexReasons.Drop(sim, id));
                if (id != null && id != "yok") h.Add(tag + "/Teleks cephe", TelexReasons.Front(sim, id));
            }
            h.Add(tag + "/Teleks figür", TelexReasons.Pick(sim));
            foreach (var sector in sim.S.Liberation.Regions.SelectMany(r => r.Sectors))
                h.Add(tag + "/Teleks baskın", TelexReasons.Raid(sim, sector.Id));
        }

        /// <summary>Her kart her bırakma hedefinde: önizleme ve ret nedeni; alınamayan kartın iletisi.</summary>
        static void Drag(Harvest h, Simulation sim, string tag)
        {
            var board = sim.Board;
            var cards = new List<(BoardCardKind kind, string id)>();
            cards.AddRange(board.People.Select(p => (BoardCardKind.Person, p.Id)));
            cards.AddRange(board.Equipment.Select(e => (BoardCardKind.Item, e.Id)));
            cards.AddRange(PlanningBoardData.MaterialKinds.Select(k => (BoardCardKind.Material, k)));
            cards.AddRange(PlanningBoardData.MaterialKinds.Select(k => (BoardCardKind.Chip, BoardUiState.ChipId(k, 0))));
            cards.AddRange(sim.BoardRecipes.Select(r => (BoardCardKind.Recipe, r.Id)));
            cards.AddRange(board.Orders.Select(o => (BoardCardKind.Recipe, o.Id)));

            var targets = new List<BoardTarget> { BoardTarget.Bosta(), BoardTarget.Store(), BoardTarget.Hurda(), BoardTarget.Order(), BoardTarget.Book() };
            targets.AddRange(BoardLayout.Jobs.Select(BoardTarget.Pocket));
            targets.AddRange(Enumerable.Range(0, PlanningBoardData.Subjects.Length).Select(BoardTarget.Topic));
            targets.AddRange(board.People.Where(p => p.Alive).Select(p => BoardTarget.Person(p.Id)));

            foreach (var (kind, id) in cards)
            {
                h.Add(tag + "/Alınamaz", BoardDropRules.CanPick(kind, id, sim));
                foreach (var target in targets) h.Walk(tag + "/Önizleme", BoardPreviewText.For(kind, id, target, sim));
            }
        }

        /// <summary>Oyun sonu: her son için rapor, teleks ve alınamayan kart. Son geçici yazılır ve geri alınır; simülasyon bu çağrıdan sonra kullanılmaz.</summary>
        static void Endings(Harvest h, Simulation sim)
        {
            foreach (var ending in GameData.Endings)
            {
                sim.S.Ending = ending.Id;
                h.Walk("Son " + ending.Id, FinalReportBuilder.Build(sim));
                h.Add("Son " + ending.Id, BoardDropRules.CanPick(BoardCardKind.Person, sim.Board.People[0].Id, sim));
                h.Add("Son " + ending.Id, TelexReasons.Pick(sim));
                sim.S.Ending = null;
            }
        }

        // ================================================================== toplayıcı

        /// <summary>Veri nesnelerinin her dizgesi (metin → ilk görüldüğü yol). Yalnız <c>Corrigible.Desk</c> türlerine ve değer demetlerine iner.</summary>
        sealed class Harvest
        {
            const int MaxDepth = 12;

            readonly Dictionary<string, string> texts = new Dictionary<string, string>();
            readonly HashSet<object> seen = new HashSet<object>(ByReference.Instance);
            readonly HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

            public int Count => texts.Count;

            public void Add(string path, string text)
            {
                if (!string.IsNullOrEmpty(text) && !texts.ContainsKey(text)) texts[text] = path;
            }

            /// <summary>Panodaki ve kapıdaki kişilerin adları (ölüler ve ayrılanlar da birikir), yazıldıkları biçimlerle.</summary>
            public void LearnNames(Simulation sim)
            {
                var people = sim.Board.People.Select(p => p.Name).ToList();
                var candidate = sim.Admission?.Candidate?.Resident?.Name;
                if (candidate != null) people.Add(candidate);
                foreach (string name in people.Where(n => !string.IsNullOrEmpty(n)))
                {
                    names.Add(name);
                    names.Add(DeskText.Upper(name, DeskLang.En));
                    names.Add(DeskText.Upper(name, DeskLang.Tr));
                    names.Add(PlanningBoardSnapshotBuilder.Lowered(name, DeskLang.En));
                    names.Add(PlanningBoardSnapshotBuilder.Lowered(name, DeskLang.Tr));
                }
            }

            public void Walk(string path, object value, int depth = 0)
            {
                if (value == null || depth > MaxDepth) return;
                if (value is string text) { Add(path, text); return; }
                if (value is IDictionary map)
                {
                    foreach (DictionaryEntry e in map) Walk(path + "[" + e.Key + "]", e.Value, depth + 1);
                    return;
                }
                if (value is IEnumerable list)
                {
                    int i = 0;
                    foreach (var item in list) Walk(path + "[" + i++ + "]", item, depth + 1);
                    return;
                }
                var type = value.GetType();
                if (!Descends(type)) return;
                if (!type.IsValueType && !seen.Add(value)) return;
                for (var t = type; t != null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType)
                    foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (!IsId(field.Name)) Walk(path + "." + field.Name, field.GetValue(value), depth + 1);
            }

            static bool Descends(Type type)
            {
                if (type.IsPrimitive || type.IsEnum || typeof(Delegate).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
                string full = type.FullName ?? string.Empty;
                return full.StartsWith("Corrigible.Desk", StringComparison.Ordinal) || full.StartsWith("System.ValueTuple", StringComparison.Ordinal);
            }

            static bool IsId(string field)
            {
                int backing = field.IndexOf('>');                  // <Ad>k__BackingField
                if (backing > 1) field = field.Substring(1, backing - 1);
                return IdFields.Contains(field) || field.EndsWith("Id", StringComparison.Ordinal) || field.EndsWith("Ids", StringComparison.Ordinal)
                    || field.EndsWith("Key", StringComparison.Ordinal) || field.EndsWith("Color", StringComparison.Ordinal);
            }

            /// <summary>Türkçe kalan metinler ("yol: metin"). Adlar çıkarıldıktan sonra harf ve sözcük; tablo anahtarı olan kaynak olduğu gibi.</summary>
            public List<string> Turkish(IReadOnlyDictionary<string, string> english)
            {
                var translated = new HashSet<string>(english.Values, StringComparer.Ordinal);
                var order = names.OrderByDescending(n => n.Length).ToArray();
                var found = new List<string>();
                foreach (var pair in texts)
                {
                    string rest = pair.Key;
                    foreach (string name in order) rest = rest.Replace(name, string.Empty);
                    bool source = english.TryGetValue(pair.Key, out var en) && en != pair.Key && !translated.Contains(pair.Key);
                    if (source || TurkishLetters.IsMatch(rest) || TurkishWords.IsMatch(rest)) found.Add(pair.Value + ": " + pair.Key);
                }
                found.Sort(StringComparer.Ordinal);
                return found;
            }
        }

        sealed class ByReference : IEqualityComparer<object>
        {
            public static readonly ByReference Instance = new ByReference();
            bool IEqualityComparer<object>.Equals(object a, object b) => ReferenceEquals(a, b);
            int IEqualityComparer<object>.GetHashCode(object o) => RuntimeHelpers.GetHashCode(o);
        }
    }
}
