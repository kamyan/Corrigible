using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kartlı panonun anlık görüntüsünü simülasyondan kurar (Revizyon 3 · R15/R16; PLAN §4). Bütün görünür metin burada, o anki
    /// dilde (<see cref="Loc"/>) üretilir; sayılar <see cref="DeskFormat"/> ile (TR virgül, EN nokta, eksi U+2212), büyük harf
    /// <see cref="DeskText.Upper(string, DeskLang)"/> ile. Saf okuma: Core durumunu değiştirmez, RNG çekmez, komut yazmaz; çağıran
    /// yalnız değişimde (oyun <c>Changed</c>, yükleme, dil, poz) çağırır.
    /// <para>
    /// <see cref="BoardSnapshot.People"/>: önce canlılar (<c>Bd.People</c> sırasıyla), sonra ölenler (<c>Lost = true</c>; görünüm
    /// yalnız zaten gösterdiği kartta KAYIP durumunu oynatır, ölü için yeni kart açmaz). Cep ve deste yalnız canlıları içerir.
    /// <see cref="BoardSnapshot.Items"/>: yuva sırasına göre (kit, zırh, yardımcı), aynı yuvada Core'un oluşturma sırası; bir kişinin
    /// eşyaları içindeki sıra, kişinin arkasındaki sıkıştırılmış yığın sırasıdır.
    /// </para>
    /// <para>
    /// Aşama 5 · R18 (PLAN §1.5–§1.11, [B]): <see cref="BoardSnapshot.Recipes"/> defterin altı sayfası (<c>BoardRecipes</c> sırası; denge
    /// değerlerinden), <see cref="BoardSnapshot.Order"/> iş emri formu (Core'un ilk emri; durum satırı, DURDU damgası ve TARİF kutusundaki
    /// kart; tahmin <see cref="ProductionForecast"/>), fişler <see cref="BoardDropRules.ChipCount"/>'tan, <see cref="BoardSnapshot.NewItem"/>
    /// son kapanışın ürünü (<see cref="NewItemOf"/>). Kurucu <see cref="BoardUiState"/>'i değiştirmez; günün yeni ürününü sabitlemek ve
    /// artık olmayan emir ve fişlerin yerlerini silmek sunucunun <see cref="SyncUi"/> çağrısıdır.
    /// </para>
    /// <para>
    /// Aşama 6 · R17f (PLAN §1.16): kişi dosyasının BULGULAR satırları Core'un bulgu kodlarından (<see cref="Simulation.AdmissionFindingCode"/>)
    /// sunum ifadesi, İZLER bölümü, GEÇMİŞ'te inceleme fişi olayı sunum ifadesiyle, KARAR satırı ve kıskaçlı etiket
    /// (<see cref="FolderData.TagText"/>). Dosyanın hiçbir satırı "robot" demez.
    /// </para>
    /// </summary>
    public static class PlanningBoardSnapshotBuilder
    {
        /// <summary>İş emri üstünde çizilen en çok fiş (havuz 8; tam sayı stok kartında "ayrı n").</summary>
        public const int ChipCap = 8;

        /// <summary>Kit yığınında çizilen en çok kart (en yenisi üstte).</summary>
        public const int KitStackDrawn = 3;

        static string[] Subjects => PlanningBoardData.Subjects;

        public static BoardSnapshot Build(Simulation sim, BoardUiState ui, bool english, bool inputBlocked, bool storyHold)
        {
            var lang = english ? DeskLang.En : DeskLang.Tr;
            ui = ui ?? new BoardUiState();
            var s = new BoardSnapshot { English = english };
            FillWords(s.Words);
            s.Header = s.LockReason = s.EntryLine = s.HurdaLine = s.HurdaPlateLine = s.LostCover = string.Empty;
            s.FolderCoverLine = Loc.T("— kart seç —");
            s.Order.Number = s.Order.Status = s.Order.Stamp = s.Order.StopReason = s.Order.DropHint = string.Empty;
            var board = sim?.Board;
            FillMaterials(s, sim, board, lang);
            FillKits(s, board);
            FillBook(s, sim, lang);
            if (board == null)
            {
                FillTopics(s, sim, null, ui, lang);
                return s;
            }

            int alive = board.People.Count(p => p.Alive), capacity = sim.B.boardResidentCapacity;

            // ---------------- başlık, kilit, plaka satırları
            s.Header = Loc.F("V.{0:000} · erzak {1} ({2}) · {3}/{4} kişi", sim.S.Day + 1, (int)Math.Floor(sim.Shelter?.Supplies ?? 0),
                DeskFormat.Signed(sim.BoardFoodPreview, 1, lang), alive, capacity);
            s.Locked = board.Locked;
            s.LockReason = LockReason(sim) ?? string.Empty;
            s.Editable = !board.Locked && !inputBlocked && !storyHold && sim.S.Ending == null && sim.S.Raid == null;
            s.EntryLine = !board.Locked ? Loc.F("PLAN AÇIK · {0}/{1} kişi", alive, capacity)
                : s.LockReason.Length > 0 ? Loc.F("PLAN KİLİTLİ · {0}", s.LockReason) : Loc.T("PLAN KİLİTLİ");
            int scrap = Math.Max(0, sim.B.boardScrapMetal), metalCap = Math.Max(0, sim.B.boardMaterialsCapacity);
            s.HurdaPlateLine = Loc.F("1 eşya → {0} metal", scrap);
            s.HurdaLine = (board.Material("metal")?.Quantity ?? 0) >= metalCap
                ? Loc.F("{0} · {1}", s.HurdaPlateLine, Loc.F("metal deposu dolu ({0}): kazanç yok", metalCap))
                : s.HurdaPlateLine;

            // ---------------- cepler ve deste (canlılar)
            var place = new Dictionary<string, string>();
            foreach (string job in BoardLayout.Jobs)
            {
                var occupant = BoardDropRules.Occupant(board, job);
                if (occupant == null) continue;
                s.PocketOccupant[job] = occupant.Id;
                place[occupant.Id] = job;
            }
            var later = new List<string>();
            foreach (var p in board.People)
            {
                if (!p.Alive || place.ContainsKey(p.Id)) continue;
                place[p.Id] = "rest";
                // K4-A: üstte Boşta'nın ilk uygun kişisi (yatan ve kısıtlı olmayan); sonra kalanlar, ikisi de Bd.People sırasıyla
                if (p.Down <= 0 && !p.Restricted) s.DeckOrder.Add(p.Id); else later.Add(p.Id);
            }
            s.DeckOrder.AddRange(later);

            // ---------------- kişi kartları
            var squad = sim.PlanningSquad();
            var letters = MasteryLetters();
            foreach (var p in board.People.Where(r => r.Alive).Concat(board.People.Where(r => !r.Alive)))
                s.People.Add(PersonCard(sim, p, place.TryGetValue(p.Id, out var at) ? at : "rest", squad, letters, ui, s.Words));

            // ---------------- eşyalar (yuva sırası, sonra oluşturma sırası; OrderBy kararlıdır)
            foreach (var e in board.Equipment.OrderBy(x => SlotRank(x.Kind)))
                s.Items.Add(ItemCard(sim, board, e, s.Words));

            FillChips(s, board, lang);
            FillTopics(s, sim, board, ui, lang);
            FillOrder(s, sim, board, lang);
            s.NewItem = NewItemOf(sim, ui);
            s.RecipesDraggable = s.Editable;

            // ---------------- seçim ve kişi dosyası
            var selected = string.IsNullOrEmpty(ui.SelectedId) ? null : board.Person(ui.SelectedId);
            if (selected != null)
            {
                s.SelectedId = selected.Id;
                s.FolderCoverLine = DeskText.Upper(selected.Name, lang);
                s.Folder = Folder(sim, selected, s.Person(selected.Id), s.Words, lang, inputBlocked, storyHold);
            }
            else if (!string.IsNullOrEmpty(ui.SelectedId) && board.EquipmentItem(ui.SelectedId) != null) s.SelectedId = ui.SelectedId;

            // ---------------- KAYIPLAR (ölenler; ayrılanlar Bd.People'da değildir)
            foreach (var p in board.People)
            {
                if (p.Alive) continue;
                s.Lost.Add(new LostEntry { Id = p.Id, Name = p.Name, Line = LostLine(sim, p) });
            }
            s.LostCover = s.Lost.Count > 0 ? Loc.F("KAYIPLAR · {0}", s.Lost.Count) : string.Empty;
            return s;
        }

        // ---------------------------------------------------------------- herkese açık yardımcılar

        /// <summary>
        /// Kilidin nedeni (kilit etiketinin 2. satırı): basılı devir fişi önce gelir; yoksa süren ya da bu vardiya çözülmüş baskın
        /// (baskın çözülünce plan vardiya sonuna kadar kilitli kalır). Kilit yoksa null.
        /// </summary>
        public static string LockReason(Simulation sim)
        {
            var board = sim?.Board;
            if (board == null || !board.Locked) return null;
            if (board.Printed) return Loc.T("devir fişi basıldı");
            var log = sim.S.RaidLog;
            if (sim.S.Raid != null || log.Count > 0 && log[log.Count - 1].Day == sim.S.Day) return Loc.T("baskın emri verildi");
            return null;
        }

        /// <summary>Eğitim konusunun kısa adı (karışık harf): Teknisyen, EMP, Analiz, Üretim, Mutfak, Bakım, İkmal.</summary>
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

        /// <summary>Konu satırının üzerine gelme satırı: eğitimdeki kişi o konuda ustaysa "usta" (renk tek başına durum taşımaz).</summary>
        public static string TopicHoverLine(TopicsData topics, int row) =>
            topics != null && row >= 0 && row < topics.Mastered.Length && topics.Mastered[row] ? Loc.T("usta") : null;

        /// <summary>
        /// Portre anahtarı: ad ASCII küçük harfe katlanır ("İpek" → "ipek"), sayısal ek atılır ("Suna 12" → "suna"; portre adı
        /// izler, robot durumunu asla).
        /// </summary>
        public static string PortraitKeyOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string n = name.Trim();
            int space = n.LastIndexOf(' ');
            if (space > 0 && space < n.Length - 1 && n.Substring(space + 1).All(char.IsDigit)) n = n.Substring(0, space).TrimEnd();
            var sb = new StringBuilder(n.Length);
            foreach (char c in n)
            {
                switch (c)
                {
                    case 'ç': case 'Ç': sb.Append('c'); break;
                    case 'ğ': case 'Ğ': sb.Append('g'); break;
                    case 'ı': case 'İ': case 'I': case 'î': case 'Î': sb.Append('i'); break;
                    case 'ö': case 'Ö': sb.Append('o'); break;
                    case 'ş': case 'Ş': sb.Append('s'); break;
                    case 'ü': case 'Ü': case 'û': case 'Û': sb.Append('u'); break;
                    case 'â': case 'Â': sb.Append('a'); break;
                    default: sb.Append(char.ToLowerInvariant(c)); break;
                }
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- kişi

        static PersonCardData PersonCard(Simulation sim, ResidentState p, string place, List<SquadSlot> squad, string[] letters,
            BoardUiState ui, BoardWords words)
        {
            var d = new PersonCardData
            {
                Id = p.Id, Name = p.Name, Job = Loc.T(p.Profession), PortraitKey = PortraitKeyOf(p.Name), JobId = place,
            };
            for (int i = 0; i < d.MasteryLetters.Length && i < Subjects.Length; i++)
            {
                d.MasteryLetters[i] = i < letters.Length ? letters[i] : string.Empty;
                d.MasteryTiers[i] = sim.BoardSkillTier(p, Subjects[i]);
            }
            d.StatLabels[0] = Loc.T("CAN");
            d.StatLabels[1] = Loc.T("HAR");
            d.StatLabels[2] = Loc.T("GÜÇ");
            // Saha cebinde baskının gerçek değerleri (ustalık artısı, tavan), başka yerde temel değerler
            var slot = p.Alive && IsField(place) ? squad.FirstOrDefault(x => x.PersonId == p.Id) : null;
            d.StatValues[0] = Loc.F("{0}/{1}", p.Hp, p.MaxHp);
            d.StatValues[1] = DeskFormat.Int(slot?.Move ?? p.Move);
            d.StatValues[2] = DeskFormat.Int(slot?.Power ?? p.Power);
            d.Hurt = p.Alive && p.Hp < p.MaxHp;
            Strip(sim, p, place, out d.Strip, out d.StripTone);
            var file = sim.Admission?.File(p.Id);
            d.Injured = p.Alive && p.Hp < p.MaxHp;
            d.Bedridden = p.Alive && p.Down > 0;
            d.Restricted = p.Alive && p.Restricted;
            // İNCELE: dosya incelemede (yeni talimat eşleşti ya da inceleme emri bekliyor); robot bilgisi taşımaz
            d.Review = p.Alive && file != null && (file.ReviewNeeded || file.ReviewPending);
            d.Lost = !p.Alive;
            d.Selected = ui.SelectedId == p.Id;
            d.HoverTitle = p.Name;
            d.HoverLine = d.Review ? Loc.F("{0} · {1}", d.Strip, words.Review) : d.Strip;
            return d;
        }

        /// <summary>Kart şeridi, ilk eşleşen: kayıp, yatıyor, kısıtlı, eğitim, boşta, cep adı (karışık harf, ≤ 66 mm).</summary>
        static void Strip(Simulation sim, ResidentState p, string place, out string text, out StripTone tone)
        {
            if (!p.Alive) { text = LostLine(sim, p); tone = StripTone.Ink; return; }
            if (p.Down > 0) { text = Loc.F("Yatıyor · {0} v.", p.Down); tone = StripTone.Blue; return; }
            if (p.Restricted)
            {
                int left = (sim.Admission?.File(p.Id)?.RestrictedUntil ?? -1) - sim.S.Day;
                text = left > 0 ? Loc.F("Kısıtlı · {0} v.", left) : Loc.T("Kısıtlı · bitti");
                tone = StripTone.Red;
                return;
            }
            if (place == "train")
            {
                string subject = p.TrainingSubject;
                int xp = p.Skill(subject);
                int gain = Math.Max(0, Math.Min(sim.B.boardExpertAt, xp + sim.B.boardTrainingGain) - xp);
                text = Loc.F("{0} +{1}", TopicLabel(subject), gain);
                tone = StripTone.Ink;
                return;
            }
            if (place == "rest")
            {
                int heal = Math.Max(0, Math.Min(p.MaxHp, p.Hp + Math.Max(0, sim.B.boardRestHeal)) - p.Hp);
                text = heal > 0 ? Loc.F("Boşta · +{0} can", heal) : Loc.T("Boşta");
                tone = StripTone.Mute;
                return;
            }
            text = PlanningBoardData.JobName(place);
            tone = StripTone.Ink;
        }

        /// <summary>"Kayıp · V.024" (ölüm vardiyası baskın günlüğünden); günlükte yoksa "Kayıp".</summary>
        static string LostLine(Simulation sim, ResidentState p)
        {
            var day = BoardDeathLog.DeathDay(sim, p.Id);
            return day.HasValue ? Loc.F("Kayıp · V.{0:000}", day.Value + 1) : Loc.T("Kayıp");
        }

        /// <summary>Yedi ustalık harfi (TR T E A Ü M B İ · EN T E A P K C F), tek çeviri anahtarından.</summary>
        static string[] MasteryLetters()
        {
            string source = Loc.N("T E A Ü M B İ");
            var parts = Loc.T(source).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == Subjects.Length ? parts : source.Split(' ');
        }

        static bool IsField(string job) => Array.IndexOf(PlanningBoardData.FieldJobs, job) >= 0;

        // ---------------------------------------------------------------- eşya

        static int SlotRank(string kind)
        {
            string slot = PlanningBoardData.EquipmentSlot(kind);
            return slot == "kit" ? 0 : slot == "armor" ? 1 : 2;
        }

        static ItemSlotKind SlotOf(string kind)
        {
            string slot = PlanningBoardData.EquipmentSlot(kind);
            return slot == "kit" ? ItemSlotKind.Kit : slot == "armor" ? ItemSlotKind.Armor : ItemSlotKind.Aid;
        }

        static string SlotWord(string kind, BoardWords words)
        {
            switch (SlotOf(kind))
            {
                case ItemSlotKind.Kit: return words.Kit;
                case ItemSlotKind.Armor: return words.Armor;
                default: return words.Aid;
            }
        }

        static ItemCardData ItemCard(Simulation sim, PlanningBoardState board, BoardEquipment e, BoardWords words)
        {
            string owner = string.IsNullOrEmpty(e.OwnerId) ? null : e.OwnerId;
            var d = new ItemCardData
            {
                Id = e.Id, Kind = e.Kind, Name = PlanningBoardData.EquipmentName(e.Kind), Slot = SlotOf(e.Kind),
                SlotLabel = SlotWord(e.Kind, words), UseLine = UseLine(sim, e.Kind), OwnerId = owner,
            };
            d.HoverTitle = d.Name;
            string where = owner == null ? Loc.T("DEPO") : board.Person(owner)?.Name ?? Loc.T("DEPO");
            d.HoverLine = string.IsNullOrEmpty(d.UseLine) ? where : Loc.F("{0} · {1}", where, d.UseLine);
            return d;
        }

        /// <summary>Kullanım satırı, Core'un gerçek kuralı (D7: bakım kiti panoda etkisiz; EMP hücresi yalnız EMP uzmanında).</summary>
        public static string UseLine(Simulation sim, string kind)
        {
            switch (kind)
            {
                case "kit_tek": case "kit_emp": case "kit_ana":
                    return Loc.F("saha rolü: {0}", PlanningBoardData.JobName(kind.Substring(4)));
                case "armor":
                    return Loc.T("ilk hasarı karşılar, kırılır");
                case "emp_cell":
                {
                    int shots = Math.Max(1, sim?.B.boardEmpBoostCharges ?? 1), damage = Math.Max(0, sim?.B.boardEmpBoostDamage ?? 1);
                    return shots == 1 ? Loc.F("EMP uzmanı: ilk atış +{0} hasar; baskında tükenir", damage)
                        : Loc.F("EMP uzmanı: ilk {0} atış +{1} hasar; baskında tükenir", shots, damage);
                }
                case "medkit":
                    return Loc.F("bakım görevinde +{0} can; panoda bakım cebi yok", Math.Max(0, sim?.B.boardMedkitHeal ?? 1));
                default:
                    return string.Empty;
            }
        }

        // ---------------------------------------------------------------- depo, fişler, konular

        static void FillMaterials(BoardSnapshot s, Simulation sim, PlanningBoardState board, DeskLang lang)
        {
            for (int i = 0; i < s.Materials.Length && i < BoardLayout.MaterialKinds.Length; i++)
            {
                string kind = BoardLayout.MaterialKinds[i];
                int stock = board?.Material(kind)?.Quantity ?? 0;
                int staged = board?.ProductionInputs.Where(m => m.Kind == kind).Sum(m => m.Quantity) ?? 0;
                int ordered = board?.Orders.Where(o => !o.Started).Sum(o => o.Reserves.Where(r => r.Kind == kind).Sum(r => r.Quantity)) ?? 0;
                int free = board == null ? 0 : sim.BoardMaterialFree(kind);
                // "ayrı" = üretime konan + iş emrine ayrılan (stok − serbest değil: serbestteki max(0) aşırı ayrımı gizler)
                int held = staged + ordered;
                string name = PlanningBoardData.MaterialName(kind);
                s.Materials[i] = new MaterialCardData
                {
                    Kind = kind, Name = DeskText.Upper(name, lang), Stock = stock, Free = free, Held = held,
                    StockLine = Loc.F("stok {0}", stock), HeldLine = Loc.F("ayrı {0}", held),
                    HoverTitle = name, HoverLine = Loc.F("serbest {0} · stok {1} · ayrı {2}", free, stock, held),
                };
            }
        }

        static void FillKits(BoardSnapshot s, PlanningBoardState board)
        {
            for (int i = 0; i < s.Kits.Length && i < BoardLayout.KitKinds.Length; i++)
            {
                string kind = BoardLayout.KitKinds[i];
                var stored = board == null ? new List<BoardEquipment>()
                    : board.Equipment.Where(e => string.IsNullOrEmpty(e.OwnerId) && e.Kind == kind).ToList();
                var k = new KitStackData { Kind = kind, Count = stored.Count, CountText = Loc.F("×{0}", stored.Count) };
                for (int j = Math.Max(0, stored.Count - KitStackDrawn); j < stored.Count; j++) k.TopIds.Add(stored[j].Id);
                s.Kits[i] = k;
            }
        }

        /// <summary>
        /// İş emrine ayrılan (ya da eski kayıtta konan) her birim bir fiş (<see cref="BoardDropRules.ChipCount"/>, PLAN §1.8; başlamış
        /// emirde girdi ödendiği için fiş yok); en çok 8 çizilir, türler sırayla paylaşır (her türün en az bir fişi görünür).
        /// </summary>
        static void FillChips(BoardSnapshot s, PlanningBoardState board, DeskLang lang)
        {
            var kinds = BoardLayout.MaterialKinds;
            var staged = new int[kinds.Length];
            var drawn = new int[kinds.Length];
            for (int i = 0; i < kinds.Length; i++) staged[i] = board == null ? 0 : Math.Max(0, BoardDropRules.ChipCount(board, kinds[i]));
            int total = 0;
            for (bool more = true; more && total < ChipCap;)
            {
                more = false;
                for (int i = 0; i < kinds.Length && total < ChipCap; i++)
                    if (drawn[i] < staged[i]) { drawn[i]++; total++; more = true; }
            }
            for (int i = 0; i < kinds.Length; i++)
            {
                string name = DeskText.Upper(PlanningBoardData.MaterialName(kinds[i]), lang);
                for (int j = 0; j < drawn[i]; j++)
                    s.Chips.Add(new ChipData { Id = BoardUiState.ChipId(kinds[i], j), Kind = kinds[i], Name = name, Index = j });
            }
        }

        static void FillTopics(BoardSnapshot s, Simulation sim, PlanningBoardState board, BoardUiState ui, DeskLang lang)
        {
            var trainee = BoardDropRules.Occupant(board, "train");
            var t = s.Topics;
            for (int i = 0; i < t.Labels.Length && i < Subjects.Length; i++)
            {
                t.Subjects[i] = Subjects[i];
                t.Labels[i] = DeskText.Upper(TopicLabel(Subjects[i]), lang);
                t.Mastered[i] = trainee != null && trainee.Skill(Subjects[i]) >= sim.B.boardExpertAt;
            }
            t.RingRow = Math.Max(0, Array.IndexOf(Subjects, BoardDropRules.TrainingSubject(sim, ui)));
            t.TraineeId = trainee?.Id;
        }

        static void FillWords(BoardWords w)
        {
            w.Injured = Loc.T("YARALI");
            w.Bedridden = Loc.T("YATIYOR");
            w.Restricted = Loc.T("KISITLI");
            w.Review = Loc.T("İNCELE");
            w.Lost = Loc.T("KAYIP");
            w.Free = Loc.T("serbest");
            w.Kit = Loc.T("KİT");
            w.Armor = Loc.T("ZIRH");
            w.Aid = Loc.T("YARDIMCI");
        }

        // ---------------------------------------------------------------- tarif defteri (Aşama 5 · R18a)

        /// <summary>
        /// Defterin sözcükleri, kapalı defterin üzerine gelme satırları ve altı sayfa (<c>BoardRecipes</c> sırası: armor, medkit,
        /// emp_cell, kit_tek, kit_emp, kit_ana; açılım <c>i / 2</c>, sayfa <c>i % 2</c>). Sayılar ve süre denge değerlerinden.
        /// </summary>
        static void FillBook(BoardSnapshot s, Simulation sim, DeskLang lang)
        {
            var w = s.Book;
            w.Header = Loc.T("KYGA · TARİF DEFTERİ");
            w.Output = Loc.T("ÇIKTI");
            w.Input = Loc.T("GİRDİ");
            w.Time = Loc.T("SÜRE");
            w.Next = Loc.T("sonraki ►");
            w.Prev = Loc.T("◄ önceki");
            w.Tab = Loc.T("İŞ EMRİ");
            // BoardRecipes her okumada yeni liste kurar: bir kez alınır (PLAN §0)
            var recipes = sim?.BoardRecipes ?? new List<BoardRecipe>();
            int spreads = Math.Max(1, (recipes.Count + 1) / 2);
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                var d = new RecipeData
                {
                    Id = r.Id, Name = r.Name, Title = DeskText.Upper(r.Name, lang), Index = i, Turns = r.Turns,
                    Number = Loc.F("TARİF {0:00}", i + 1),
                    SlotWord = SlotPhrase(r.Output),
                    OutLine = Loc.T("1 ×"),
                    TurnsLine = TurnsLine(r.Turns),
                    Note = RecipeNote(sim, r),
                    PageLabel = Loc.F("s. {0}", i + 1),
                    SpreadLabel = Loc.F("AÇILIM {0}/{1}", i / 2 + 1, spreads),
                };
                AddInputs(d.Inputs, r);
                s.Recipes.Add(d);
            }
            s.BookTitle = Loc.T("TARİF DEFTERİ");
            s.BookLine = Loc.F("{0} tarif", recipes.Count);
        }

        /// <summary>Çıktının yuva sözcüğü (sayfanın ÇIKTI satırı): zırh, yardımcı ya da kit yuvası.</summary>
        static string SlotPhrase(string output)
        {
            switch (PlanningBoardData.EquipmentSlot(output))
            {
                case "kit": return Loc.T("kit yuvası");
                case "armor": return Loc.T("zırh yuvası");
                default: return Loc.T("yardımcı yuvası");
            }
        }

        /// <summary>Süre: "1 vardiya" / "1 shift", "2 vardiya" / "2 shifts" (EN tekil ve çoğul iki ayrı anahtar).</summary>
        public static string TurnsLine(int turns) => turns == 1 ? Loc.T("1 vardiya") : Loc.F("{0} vardiya", turns);

        /// <summary>
        /// Sayfanın el notu: çok vardiyalı tarifte deneyimli üretici (hız kademesi) işi tek kapanışta bitirebiliyorsa "→ deneyimli
        /// üretici: tek kapanış" (Core: kapanış başına 1 + <c>boardCraftExtraProgress</c>). [B]'de 04 sayfasının "→ eksik saha kiti önce
        /// yapılır" notu çizilmez (D8: otomatik seçim yok).
        /// </summary>
        static string RecipeNote(Simulation sim, BoardRecipe r)
        {
            int fast = 1 + Math.Max(0, sim?.B.boardCraftExtraProgress ?? 0);
            return r.Turns > 1 && fast >= r.Turns ? Loc.T("→ deneyimli üretici: tek kapanış") : string.Empty;
        }

        static void AddInputs(List<RecipeInput> list, BoardRecipe r)
        {
            foreach (var m in r.Inputs)
                list.Add(new RecipeInput
                {
                    Kind = m.Kind, Name = PlanningBoardData.MaterialName(m.Kind), Quantity = m.Quantity,
                    BookQty = Loc.F("× {0}", m.Quantity), CardQty = Loc.F("×{0}", m.Quantity),
                });
        }

        /// <summary>112 × 80 tarif kartının verisi (PLAN §1.6). <paramref name="id"/>: formdaki kartta iş emri, kopyada tarif kimliği.</summary>
        static RecipeCardData CardOf(BoardRecipe r, string id)
        {
            var c = new RecipeCardData
            {
                Id = id, RecipeId = r.Id, Name = r.Name, IconKey = r.Output, Turns = r.Turns,
                TurnsLine = TurnsLine(r.Turns), OutLine = Loc.T("→ 1 ürün"), NeedsLabel = Loc.T("GİRDİ"),
            };
            AddInputs(c.Inputs, r);
            return c;
        }

        // ---------------------------------------------------------------- iş emri formu (Aşama 5 · R18c, [B])

        /// <summary>
        /// ÜRETİM · İŞ EMRİ formu (PLAN §1.7 [B]): Core'un ilk (tek) emri. Boş · tarif (ayrılan 0) · eksik · tam · başladı · durdu.
        /// Eksik notu tarifin her girdisi için <c>ihtiyaç − ayrılan</c> (Core'un <c>BoardPlanCheck</c>'iyle aynı koşul; şalter kilidi
        /// Aşama 4'te). DURDU yalnız girdileri hazır ya da başlamış emirde, <see cref="ProductionForecast"/>'ın üç nedeniyle (üretici
        /// yok, açlık, eşya deposu dolu). İlerleme kutuları yalnız çok vardiyalı tarifte (D7): dolu = <c>Turns − Remaining</c>.
        /// </summary>
        static void FillOrder(BoardSnapshot s, Simulation sim, PlanningBoardState board, DeskLang lang)
        {
            var d = s.Order;
            d.Number = Loc.F("No {0:000}", sim.S.Day + 1);
            d.State = OrderState.Empty;
            var order = board.Orders.FirstOrDefault();
            if (order == null) return;
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
            if (recipe == null) return;   // Core: "İş emrinin tarifi bulunamadı." (yalnız bozuk kayıt); form boş kalır

            d.OrderId = order.Id;
            d.RecipeId = recipe.Id;
            d.Started = order.Started;
            d.Turns = recipe.Turns;
            d.Done = Math.Max(0, recipe.Turns - order.Remaining);
            d.Card = CardOf(recipe, order.Id);
            d.Card.Done = recipe.Turns > 1 ? d.Done : -1;
            d.CardDraggable = s.Editable;
            string name = Lowered(recipe.Name, lang);

            if (!order.Started)
            {
                var missing = new List<string>();
                int reserved = 0;
                foreach (var input in recipe.Inputs)
                {
                    int held = order.Reserves.Where(r => r.Kind == input.Kind).Sum(r => r.Quantity);
                    reserved += Math.Max(0, held);
                    int lack = input.Quantity - held;
                    if (lack > 0) missing.Add(Loc.F("{0} {1}", lack, Lowered(PlanningBoardData.MaterialName(input.Kind), lang)));
                }
                if (missing.Count > 0)
                {
                    d.State = reserved == 0 ? OrderState.Recipe : OrderState.Missing;
                    d.Status = Loc.F("eksik: {0}", JoinPlus(missing));
                    d.DropHint = OrderDropHint(d.State, s.Editable);
                    return;
                }
            }

            var outlook = ProductionForecast.Of(sim);
            if (outlook.Stop != ProductionStop.None)
            {
                d.State = OrderState.Stopped;
                d.Stamp = Loc.T("DURDU");
                d.StopReason = outlook.StopReason ?? string.Empty;
                d.Status = Loc.F("{0} · {1}", name, d.StopReason);
                return;
            }
            string maker = outlook.Maker ?? string.Empty;
            if (order.Started)
            {
                d.State = OrderState.Started;
                d.Status = Loc.F("{0} · girdiler ödendi · {1}/{2} · üretici: {3}", name, d.Done, d.Turns, maker);
            }
            else
            {
                d.State = OrderState.Full;
                d.Status = Loc.F("{0} · girdiler ayrıldı · üretici: {1}", name, maker);
            }
        }

        /// <summary>
        /// Girdi alanının ipucu (R18c, D-F). Yalnız hammadde kabul eden taslakta: tarifi konmuş, girdisi eksik (<see cref="OrderState.Recipe"/>,
        /// <see cref="OrderState.Missing"/>) ve plan düzenlenebilir. Boş form hammadde almaz; tam, başlamış ve durmuş emirde ayıracak girdi kalmaz.
        /// </summary>
        public static string OrderDropHint(OrderState state, bool editable) =>
            editable && (state == OrderState.Recipe || state == OrderState.Missing) ? Loc.T("GİRDİLER (hammaddeyi buraya bırak)") : string.Empty;

        /// <summary>"2 metal + 1 kumaş": parçalar <c>"{0} + {1}"</c> ile soldan birleşir.</summary>
        public static string JoinPlus(IReadOnlyList<string> parts)
        {
            if (parts == null || parts.Count == 0) return string.Empty;
            string text = parts[0];
            for (int i = 1; i < parts.Count; i++) text = Loc.F("{0} + {1}", text, parts[i]);
            return text;
        }

        /// <summary>
        /// Cümle içi küçük harf ("Zırh plakası" → "zırh plakası", "Kumaş" → "kumaş"; TR İ/ı kuralı). Kısaltmayla başlayan ad
        /// ("EMP hücresi", "EMP cell") olduğu gibi kalır.
        /// </summary>
        public static string Lowered(string text, DeskLang lang)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            if (text.Length > 1 && char.IsUpper(text[1])) return text;
            return DeskText.Lower(text.Substring(0, 1), lang) + text.Substring(1);
        }

        // ---------------------------------------------------------------- yeni ürün (Aşama 5 · R18d)

        /// <summary>
        /// Son kapanışın ürünü (PLAN §1.11): <c>Board.LastCompleted</c>'teki tür için depodaki (<c>OwnerId == null</c>) en büyük
        /// numaralı eşya. Gün başına bir kez sabitlenir: <see cref="BoardUiState.NewItem"/> bugüne aitse onun kimliği okunur (aynı gün
        /// depoya sonradan giren aynı türden eşya etiketi çalmaz), değilse aynı kural yeniden hesaplanır. Etikete dokunulduysa
        /// (<c>Touched</c>), eşya depoda değilse ya da ürün yoksa (ör. bakım kiti bakımda tüketildi) null. Saf.
        /// </summary>
        public static NewItemData NewItemOf(Simulation sim, BoardUiState ui)
        {
            var board = sim?.Board;
            if (board == null) return null;
            int day = sim.S.Day;
            var state = ui?.NewItem != null && ui.NewItem.Day == day ? ui.NewItem : null;
            if (state != null && state.Touched) return null;
            string id = state != null ? state.Id : NewestStoredProduct(board);
            if (string.IsNullOrEmpty(id)) return null;
            var e = board.EquipmentItem(id);
            if (e == null || !string.IsNullOrEmpty(e.OwnerId) || !board.LastCompleted.Contains(e.Kind)) return null;
            int stack = Array.IndexOf(BoardLayout.KitKinds, e.Kind);
            if (stack < 0) return null;
            return new NewItemData { ItemId = e.Id, Kind = e.Kind, Stack = stack, Day = day };
        }

        /// <summary>
        /// <c>LastCompleted</c>'in ilk türü için depodaki en büyük numaralı eşya (<c>esya-N</c>, sayısal sonekle: esya-10 &gt; esya-9);
        /// yoksa null.
        /// </summary>
        public static string NewestStoredProduct(PlanningBoardState board)
        {
            if (board == null) return null;
            foreach (string kind in board.LastCompleted)
            {
                BoardEquipment best = null;
                long bestNumber = long.MinValue;
                foreach (var e in board.Equipment)
                {
                    if (e.Kind != kind || !string.IsNullOrEmpty(e.OwnerId)) continue;
                    long n = EquipmentNumber(e.Id);
                    if (best == null || n > bestNumber) { best = e; bestNumber = n; }
                }
                if (best != null) return best.Id;
            }
            return null;
        }

        /// <summary>"esya-12" → 12; sonek sayı değilse −1.</summary>
        public static long EquipmentNumber(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int dash = id.LastIndexOf('-');
            return dash >= 0 && long.TryParse(id.Substring(dash + 1), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out long n) ? n : -1;
        }

        /// <summary>
        /// Sunum durumunu simülasyona uydurur (sunucu çizimden önce çağırır; değiştiyse yerleşimi yazar): yeni günün ürünü bir kez
        /// sabitlenir (<see cref="BoardUiState.NewItem"/>; kayış ve etiket durumu sıfırdan), artık olmayan iş emirlerinin tarif kartı
        /// yerleri ve çizilmeyen fişlerin yerleri (sıra ≥ <see cref="BoardDropRules.ChipCount"/>) silinir. Kalan fişler yerinde durur.
        /// Simülasyonu değiştirmez, RNG çekmez. Bir şey değiştiyse true.
        /// </summary>
        public static bool SyncUi(Simulation sim, BoardUiState ui)
        {
            var board = sim?.Board;
            if (ui == null || board == null) return false;
            bool changed = false;
            int day = sim.S.Day;
            if (ui.NewItem == null || ui.NewItem.Day != day)
            {
                string id = NewestStoredProduct(board);
                var next = id == null ? null : new NewItemState { Day = day, Id = id };
                if (ui.NewItem != null || next != null) { ui.NewItem = next; changed = true; }
            }
            foreach (string orderId in ui.RecipePoses.Keys.ToList())
                if (board.Order(orderId) == null) { ui.RecipePoses.Remove(orderId); changed = true; }
            if (ui.ChipPoses != null)
                foreach (string key in ui.ChipPoses.Keys.ToList())
                    if (!PlanningBoardLayoutStore.TryParseChipKey(key, out var kind, out int index) || index >= BoardDropRules.ChipCount(board, kind))
                    {
                        ui.ChipPoses.Remove(key);
                        changed = true;
                    }
            return changed;
        }

        // ---------------------------------------------------------------- kişi dosyası

        static FolderData Folder(Simulation sim, ResidentState p, PersonCardData card, BoardWords words, DeskLang lang, bool inputBlocked, bool storyHold)
        {
            var board = sim.Board;
            var f = new FolderData { PersonId = p.Id, Name = p.Name, PortraitKey = PortraitKeyOf(p.Name), Card = card,
                Profession = Loc.T(p.Profession), Trait = Loc.T(p.Trait), Job = PlaceText(p, card.JobId),
                Header = Loc.F("KYGA · KİŞİ DOSYASI · V.{0:000}", sim.S.Day + 1), AdmissionHeader = Loc.T("KYGA · KABUL VE ERİŞİM") };
            int expert = sim.B.boardExpertAt;

            // Sol sayfa: künye, değerler, ustalık, taşıdığı eşyalar
            f.Left.Add(Section(Loc.T("KÜNYE")));
            f.Left.Add(new FolderLine(Loc.T("MESLEK"), Loc.T(p.Profession)));
            f.Left.Add(new FolderLine(Loc.T("PROFİL"), Loc.T(p.Trait)));
            f.Left.Add(new FolderLine(Loc.T("GÖREV"), PlaceText(p, card.JobId)));
            f.Left.Add(new FolderLine(Loc.T("DURUM"), card.Strip, card.StripTone));
            f.Left.Add(new FolderLine(card.StatLabels[0], card.StatValues[0], card.Hurt ? StripTone.Red : StripTone.Ink));
            f.Left.Add(new FolderLine(card.StatLabels[1], card.StatValues[1]));
            f.Left.Add(new FolderLine(card.StatLabels[2], card.StatValues[2]));
            f.Left.Add(Section(Loc.T("USTALIK")));
            for (int i = 0; i < Subjects.Length; i++)
            {
                int tier = card.MasteryTiers[i];
                var mastery = new FolderLine(TopicLabel(Subjects[i]), Loc.F("{0}/{1} · {2}", p.Skill(Subjects[i]), expert, TierWord(tier)),
                    tier > 0 ? StripTone.Ink : StripTone.Mute);
                f.Left.Add(mastery); f.Mastery.Add(mastery);
            }
            f.Left.Add(Section(Loc.T("TEÇHİZAT")));
            var items = board.Equipment.Where(e => e.OwnerId == p.Id).OrderBy(e => SlotRank(e.Kind)).ToList();
            foreach (var e in items) f.Left.Add(new FolderLine(SlotWord(e.Kind, words), PlanningBoardData.EquipmentName(e.Kind)));
            f.Gear[0] = string.Join(" / ", items.Where(e => e.Kind.StartsWith("kit_")).Select(e => PlanningBoardData.EquipmentName(e.Kind)));
            f.Gear[1] = string.Join(" / ", items.Where(e => e.Kind == "armor").Select(e => PlanningBoardData.EquipmentName(e.Kind)));
            f.Gear[2] = string.Join(" / ", items.Where(e => !e.Kind.StartsWith("kit_") && e.Kind != "armor").Select(e => PlanningBoardData.EquipmentName(e.Kind)));
            if (items.Count == 0) f.Left.Add(new FolderLine(null, Loc.T("taşıdığı eşya yok"), StripTone.Mute));

            // Sağ sayfa: kalıcı kabul dosyası (LastAccessJob yalnız robotlarda yazılır: asla gösterilmez)
            var adm = sim.Admission;
            var file = adm?.File(p.Id);
            f.Right.Add(Section(Loc.T("KALICI KİŞİ DOSYASI")));
            if (file == null) f.Right.Add(new FolderLine(null, Loc.T("dosya bulunamadı"), StripTone.Mute));
            else
            {
                var papers = file.Papers;
                if (papers != null)
                {
                    f.Right.Add(new FolderLine(Loc.T("AD"), papers.Name));
                    f.Right.Add(new FolderLine(Loc.T("MESLEK"), Loc.T(papers.Profession)));
                    f.Right.Add(new FolderLine(Loc.T("GELDİĞİ YER"), Loc.T(papers.Origin)));
                    f.Right.Add(new FolderLine(Loc.T("DÜZENLEME"), ShiftText(papers.IssuedShift)));
                    f.Right.Add(new FolderLine(Loc.T("BELGE NO"), PaperCode(papers.Serial, "K-")));
                    f.Right.Add(new FolderLine(Loc.T("SEVK NO"), PaperCode(papers.ReferralSerial, "K-")));
                    f.Right.Add(new FolderLine(Loc.T("MÜHÜR"), papers.Seal));
                    f.Right.Add(new FolderLine(Loc.T("PORTRE İZİ"), PaperCode(papers.Portrait, "P-")));
                }
                f.Right.Add(Section(Loc.T("KAYIT")));
                f.Right.Add(new FolderLine(Loc.T("KURUCU"), file.Founder ? Loc.T("evet") : Loc.T("hayır")));
                f.Right.Add(new FolderLine(Loc.T("GELİŞ"), ShiftText(file.ArrivalShift)));
                f.Right.Add(new FolderLine(Loc.T("KABUL"), ShiftText(file.AcceptedShift)));
                // Kapıdaki karar (Aşama 6, R17f): KARAR V.nnn · damganın adı; kurucularda yok
                var decision = file.Founder ? null : adm.Events.FirstOrDefault(e => e.PersonId == p.Id && (e.Kind == "admit" || e.Kind == "restricted"));
                if (decision != null)
                    f.Right.Add(new FolderLine(null, Loc.F("KARAR V.{0:000} · {1}", file.AcceptedShift + 1,
                        decision.Kind == "admit" ? Loc.T("KABUL · TAM ERİŞİM") : Loc.T("KISITLI KABUL"))));
                if (file.RestrictedUntil >= 0) f.Right.Add(new FolderLine(Loc.T("KISITLAMA BİTİŞİ"), ShiftText(file.RestrictedUntil), StripTone.Red));
                f.Right.Add(new FolderLine(Loc.T("DIŞ KAYIT"), file.RecordVerified ? Loc.T("teyitli") : Loc.T("teyitsiz")));
                f.Right.Add(new FolderLine(Loc.T("İNCELEME"),
                    file.ReviewPending ? Loc.T("bekliyor") : file.ReviewNeeded ? Loc.T("gerekli") : Loc.T("yok"),
                    file.ReviewPending || file.ReviewNeeded ? StripTone.Red : StripTone.Ink));
                f.Right.Add(new FolderLine(Loc.T("BAĞIMSIZ BULGU"), DeskFormat.Int(file.ClueCount)));
                // BULGULAR (D4/D5): Core'un bulgu kodlarından sunum ifadeleri; Core'un belge ve buğu cümleleri "robot" der, dosya demez
                var findings = Findings(sim, file);
                if (findings.Count > 0)
                {
                    f.Right.Add(Section(Loc.T("BULGULAR")));
                    f.Right.AddRange(findings);
                }
                // İZLER: bağımsız kayıtlar kısa sunum ifadesiyle (Core'un iz cümleleri GEÇMİŞ'e girmez)
                var clues = adm.Events.Where(e => e.PersonId == p.Id && (e.Kind == "clue_access" || e.Kind == "clue_component")).ToList();
                if (clues.Count > 0)
                {
                    f.Right.Add(Section(Loc.T("İZLER")));
                    foreach (var e in clues)
                        f.Right.Add(new FolderLine(ShiftText(e.Shift), e.Kind == "clue_access" ? Loc.T("İZ 1 · erişim kaydı") : Loc.T("İZ 2 · parça kaydı"),
                            e.Resolved ? StripTone.Mute : StripTone.Ink));
                }
                var history = adm.Events.Where(e => e.PersonId == p.Id && e.Kind != "clue_access" && e.Kind != "clue_component").TakeLast(8).ToList();
                if (history.Count > 0)
                {
                    f.Right.Add(Section(Loc.T("GEÇMİŞ")));
                    foreach (var e in history) f.Right.Add(new FolderLine(ShiftText(e.Shift), HistoryText(adm, e), e.Resolved ? StripTone.Mute : StripTone.Ink));
                }
                string risk = sim.ResidentAdmissionRisk(p.Id);
                if (!string.IsNullOrEmpty(risk))
                {
                    f.Right.Add(Section(Loc.T("UYARI")));
                    f.Right.Add(new FolderLine(null, risk, StripTone.Red));
                }
                // Özet ve bağımsız izler önce; belge ayrıntıları ek yapraklarda da bütünüyle okunur.
                f.DossierRight.Add(new FolderLine(Loc.T("GELİŞ"), ShiftText(file.ArrivalShift)));
                f.DossierRight.Add(new FolderLine(Loc.T("KABUL"), ShiftText(file.AcceptedShift)));
                f.DossierRight.Add(new FolderLine(Loc.T("ERİŞİM"), p.Restricted ? Loc.T("KISITLI") : Loc.T("TAM ERİŞİM")));
                f.DossierRight.Add(new FolderLine(Loc.T("İNCELEME"), file.ReviewPending ? Loc.T("bekliyor") : file.ReviewNeeded ? Loc.T("gerekli") : Loc.T("yok")));
                if (clues.Count > 0)
                {
                    f.DossierRight.Add(Section(Loc.T("İZLER")));
                    foreach (var e in clues) f.DossierRight.Add(new FolderLine(ShiftText(e.Shift), e.Kind == "clue_access" ? Loc.T("İZ 1 · erişim kaydı") : Loc.T("İZ 2 · parça kaydı")));
                }
                if (history.Count > 0)
                {
                    f.DossierRight.Add(Section(Loc.T("GEÇMİŞ")));
                    foreach (var e in history) f.DossierRight.Add(new FolderLine(ShiftText(e.Shift), HistoryText(adm, e)));
                }
                var seen = new HashSet<(string label, string value, bool section)>();
                var ordered = f.DossierRight.Concat(f.Right).Where(line => seen.Add((line.Label, line.Value, line.Section))).ToList();
                f.DossierRight.Clear(); f.DossierRight.AddRange(ordered);
                f.TagText = TagText(sim, p, file, words);
            }

            // Sekmeler: Core'un saf denetimleri; kapalıysa nedeni Core'un iletisi. İNCELE'nin gözetmeni Boşta'nın ilk uygun kişisi (K4-A,
            // biçim 15; Simulation.AdmissionWatcher, hedefin kendisi hariç); Boşta boşsa kapalı. Açıkken tek gözlem yuvası uyarısı (H2/H4)
            // AdmissionSnapshotBuilder.ReviewWarning'den, sekmenin üzerine gelme satırında (sunucu).
            f.ReviewLabel = words.Review;
            f.RestrictLabel = Loc.T("ERİŞİM");
            f.DismissLabel = DeskText.Upper(Loc.T("Tahliye"), lang);
            f.ReviewSub = Loc.T("dış kayıt · 1 gözetmen vardiyası");
            f.RestrictSub = Loc.T("kısıtla · kaldır");
            f.DismissSub = Loc.T("güvenle ayır · iki adım");
            bool open = !inputBlocked && !storyHold;
            string gate = storyHold ? Loc.T("Önce açılış belgesini kapat.") : null;
            string review = sim.ResidentReviewCheck(p.Id);
            string access = sim.ResidentRestrictionCheck(p.Id, !p.Restricted);
            string dismiss = sim.ResidentDismissCheck(p.Id);
            f.CanReview = open && review == null;
            f.CanRestrict = open && access == null;
            f.CanDismiss = open && dismiss == null;
            f.ReviewNote = gate ?? review;
            f.RestrictNote = gate ?? access;
            f.DismissNote = gate ?? dismiss;
            return f;
        }

        /// <summary>
        /// Dosyanın BULGULAR satırları: belge ve dış kayıt kodu → alan ifadesi, soğuk gözlem kodu → buğu ifadesi, görüşme → Core'un
        /// görüşme metni (robotluk içermez). Kod yalnız yapılmış incelemede vardır (<see cref="Simulation.AdmissionFindingCode"/>).
        /// </summary>
        static List<FolderLine> Findings(Simulation sim, ResidentAdmissionFile file)
        {
            var lines = new List<FolderLine>();
            string id = file.PersonId;
            string papers = AdmissionSnapshotBuilder.PaperPhrase(sim.AdmissionFindingCode(id, "papers"));
            if (papers != null) lines.Add(new FolderLine(null, papers));
            string record = AdmissionSnapshotBuilder.PaperPhrase(sim.AdmissionFindingCode(id, "record"));
            if (record != null) lines.Add(new FolderLine(Loc.T("DIŞ KAYIT"), record));
            string breath = AdmissionSnapshotBuilder.BreathPhrase(sim.AdmissionFindingCode(id, "breath"));
            if (breath != null) lines.Add(new FolderLine(null, breath));
            if (sim.AdmissionFindingCode(id, "interview") != null && !string.IsNullOrEmpty(file.Interview)) lines.Add(new FolderLine(null, Loc.T(file.Interview)));
            return lines;
        }

        /// <summary>GEÇMİŞ satırı: inceleme fişi olayı sunum ifadesiyle (Core cümlesi "robot" der), öteki olaylar Core metniyle.</summary>
        static string HistoryText(CommunityAdmissionState adm, AdmissionEvent e) =>
            e.Kind == "file_review" ? Loc.F("İnceleme fişi açıldı · talimat {0}", AdmissionSnapshotBuilder.VersionAt(adm, e.Shift)) : Loc.T(e.Text);

        /// <summary>
        /// Dosyanın kıskaçlı etiketi (R17f; D16): inceleme fişi açıksa "İNCELEME", değilse kısıtlıysa "KISITLI · n V" (kalan vardiya; plan
        /// eki), süresi dolmuşsa "KISITLI"; yoksa null.
        /// </summary>
        static string TagText(Simulation sim, ResidentState p, ResidentAdmissionFile file, BoardWords words)
        {
            if (!p.Alive || file.Departed) return null;
            if (file.ReviewNeeded) return Loc.T("İNCELEME");
            if (!p.Restricted) return null;
            int left = file.RestrictedUntil - sim.S.Day;
            return left > 0 ? Loc.F("KISITLI · {0} V", left) : words.Restricted;
        }

        static FolderLine Section(string label) => new FolderLine(label, null, StripTone.Ink, true);

        static string ShiftText(int day) => Loc.F("V.{0:000}", day + 1);

        /// <summary>
        /// Kurucu belgesinin Core alanları ham kimliği taşır ("K-sakin-3", "sakin-3"); kişi dosyasında numaraya iner ("K-003", "P-003").
        /// Yalnız gösterim: Core ve kayıt aynı kalır, kurucu olmayan belgeler ("N-17/009") olduğu gibi geçer.
        /// </summary>
        static string PaperCode(string value, string prefix)
        {
            const string founderId = "sakin-";
            int at = value?.IndexOf(founderId, StringComparison.Ordinal) ?? -1;
            if (at < 0 || !int.TryParse(value.Substring(at + founderId.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int number)) return value;
            return prefix + number.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
        }

        static string PlaceText(ResidentState p, string place)
        {
            if (!p.Alive) return Loc.T("Kayıp");
            if (place == "train") return Loc.F("{0} · {1}", PlanningBoardData.JobName("train"), TopicLabel(p.TrainingSubject));
            if (place == "rest") return Loc.T("Boşta");
            return PlanningBoardData.JobName(place);
        }

        static string TierWord(int tier)
        {
            switch (tier)
            {
                case 3: return Loc.T("usta");
                case 2: return Loc.T("deneyimli");
                case 1: return Loc.T("eğitimli");
                default: return Loc.T("acemi");
            }
        }
    }
}
