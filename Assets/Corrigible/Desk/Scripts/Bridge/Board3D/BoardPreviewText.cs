using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Bırakma önizlemesi (Revizyon 3 · R15b; PLAN §4.8): hedefin üstünde 400 ms sonra çıkan etiket. Başlık
    /// "&lt;HEDEF&gt; ← &lt;KART&gt;", gövde önce sonuçlar sonra uyarılar (uyarı varsa amber çerçeve). Geçersiz hedefte gövde Core'un
    /// iletisidir. Yalnız saf okumalar: durum değiştirmez, RNG çekmez, komut yazmaz; her üzerine gelmede çağrılabilir. Metinler
    /// o anki dilde (<see cref="Loc"/>); büyük harf <see cref="DeskText.Upper(string)"/> ile.
    /// <para>
    /// Aşama 5 · [B] (PLAN §1.6): tarif kopyası iş emrine "İŞ EMRİ ← ZIRH PLAKASI" / "girdi: 2 metal + 1 kumaş · üretici: Suna"
    /// (üretici yoksa "üretici yok", amber); dolu emre aynı başlık, gövdeye "başlamamış iş · ücretsiz iptal" ya da "harcanan girdi
    /// geri gelmez" (amber). Formdaki kart form dışına: "İŞİ İPTAL ET" + aynı iki gövdeden biri (başlamışsa amber). Fiş ve stok kartı
    /// ayırma (<c>board_reserve</c>) eski hazırlamanın (<c>board_stage</c>) sözcükleriyle.
    /// </para>
    /// </summary>
    public static class BoardPreviewText
    {
        /// <summary>Önizleme; komutsuz hedefte <see cref="DropPreview.Invalid"/>.</summary>
        public static DropPreview For(BoardCardKind kind, string id, BoardTarget t, Simulation sim) => For(kind, id, t, sim, null);

        /// <summary>Önizleme; kimse eğitimde değilse EĞİTİM konusu <see cref="BoardUiState.TrainingTopic"/>.</summary>
        public static DropPreview For(BoardCardKind kind, string id, BoardTarget t, Simulation sim, BoardUiState ui)
        {
            var board = sim?.Board;
            if (board == null) return DropPreview.Invalid;
            if (BoardDropRules.IsLayoutOnly(kind, id, t, sim))
                return new DropPreview(Loc.F("{0} ← {1}", Loc.T("İŞ EMRİ"), CardWord(kind, id, board, sim)), null, false, true);
            var plan = BoardDropRules.Plan(kind, id, t, sim, ui);
            if (plan.IsNone) return DropPreview.Invalid;
            string title = plan.Action == BoardDropAction.Cancel
                ? Loc.T("İŞİ İPTAL ET")
                : Loc.F("{0} ← {1}", TargetWord(plan, board), CardWord(kind, id, board, sim));
            string why = BoardDropRules.Reason(plan, sim);
            if (why != null) return new DropPreview(title, why, true, false);

            var notes = new List<string>();
            var warnings = new List<string>();
            switch (plan.Action)
            {
                case BoardDropAction.Assign: AssignNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Train: TrainNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Equip: EquipNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Scrap: ScrapNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Stage: StageNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Reserve: ReserveNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Queue: QueueNotes(sim, plan, notes, warnings); break;
                case BoardDropAction.Replace:
                    QueueNotes(sim, plan, notes, warnings);
                    warnings.Add(CancelWord(sim.Board.Order(plan.Job)));
                    break;
                case BoardDropAction.Cancel:
                {
                    var order = sim.Board.Order(plan.Job);
                    if (order != null && order.Started) warnings.Add(CancelWord(order));
                    else notes.Add(CancelWord(order));
                    break;
                }
            }
            notes.AddRange(warnings);
            return new DropPreview(title, Join(notes), warnings.Count > 0, true);
        }

        // ---------------------------------------------------------------- başlık

        static string TargetWord(BoardDropPlan plan, PlanningBoardState board)
        {
            switch (plan.Action)
            {
                case BoardDropAction.Assign: return PlaceWord(plan.Job);
                case BoardDropAction.Train:
                    return Loc.F("{0} · {1}", PlaceWord("train"), DeskText.Upper(PlanningBoardSnapshotBuilder.TopicLabel(plan.Job)));
                case BoardDropAction.Equip:
                    return plan.PersonId == null ? Loc.T("DEPO") : DeskText.Upper(board.Person(plan.PersonId)?.Name);
                case BoardDropAction.Scrap: return Loc.T("HURDA");
                case BoardDropAction.Stage:
                    return plan.Amount > BoardDropRules.Staged(board, plan.Id) ? Loc.T("İŞ EMRİ") : Loc.T("DEPO");
                case BoardDropAction.Reserve:
                    return plan.Amount > BoardDropRules.Reserved(board, plan.Id) ? Loc.T("İŞ EMRİ") : Loc.T("DEPO");
                case BoardDropAction.Queue:
                case BoardDropAction.Replace:
                    return Loc.T("İŞ EMRİ");
                default: return null;
            }
        }

        static string CardWord(BoardCardKind kind, string id, PlanningBoardState board, Simulation sim)
        {
            switch (kind)
            {
                case BoardCardKind.Person: return DeskText.Upper(board.Person(id)?.Name);
                case BoardCardKind.Item:
                {
                    var item = board.EquipmentItem(id);
                    return item == null ? null : DeskText.Upper(PlanningBoardData.EquipmentName(item.Kind));
                }
                case BoardCardKind.Material: return DeskText.Upper(PlanningBoardData.MaterialName(id));
                case BoardCardKind.Chip:
                    return BoardUiState.TryParseChipId(id, out var material, out _) ? DeskText.Upper(PlanningBoardData.MaterialName(material)) : null;
                case BoardCardKind.Recipe:
                {
                    var recipe = BoardDropRules.RecipeOf(sim, id);
                    return recipe == null ? null : DeskText.Upper(recipe.Name);
                }
                default: return null;
            }
        }

        /// <summary>Plaka sözcüğü: cep görevinin adı ya da BOŞTA, büyük harf.</summary>
        static string PlaceWord(string job) =>
            DeskText.Upper(BoardDropRules.IsPocketJob(job) ? PlanningBoardData.JobName(job) : Loc.T("Boşta"));

        // ---------------------------------------------------------------- gövde

        static void AssignNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var board = sim.Board;
            var mover = board.Person(plan.Id);
            string from = BoardDropRules.PlaceOf(board, mover), to = plan.Job;
            var partner = to == "rest" ? null : BoardDropRules.Occupant(board, to);
            if (partner == mover) partner = null;
            if (partner != null) notes.Add(SwapLine(board, partner, from));
            if (to == "rest")
            {
                int heal = RestHeal(sim, mover);
                if (heal > 0) notes.Add(Loc.F("kapanışta +{0} can", heal));
                if (from == "train") notes.Add(Loc.T("eğitim bırakılır"));
            }
            else
            {
                if (!IsField(to)) notes.Add(Loc.T("kit gerekmez"));
                if (mover.Hp < mover.MaxHp) warnings.Add(Loc.F("yaralı: can {0}/{1}, görevde iyileşmez", mover.Hp, mover.MaxHp));
            }
            FieldWarnings(board, mover, to, partner, from, warnings);
        }

        static void TrainNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var board = sim.Board;
            var mover = board.Person(plan.Id);
            string from = BoardDropRules.PlaceOf(board, mover);
            int xp = mover.Skill(plan.Job);
            int gain = Math.Max(0, Math.Min(sim.B.boardExpertAt, xp + sim.B.boardTrainingGain) - xp);
            notes.Add(Loc.F("kapanışta {0} +{1}", PlanningBoardSnapshotBuilder.TopicLabel(plan.Job), gain));
            if (from == "train") return;   // aynı kişi konu değiştiriyor: takas yok
            var trainee = BoardDropRules.Occupant(board, "train");
            if (trainee == mover) trainee = null;
            if (trainee != null) notes.Add(SwapLine(board, trainee, from));
            if (mover.Hp < mover.MaxHp) warnings.Add(Loc.F("yaralı: can {0}/{1}, görevde iyileşmez", mover.Hp, mover.MaxHp));
            FieldWarnings(board, mover, "train", trainee, from, warnings);
        }

        static void EquipNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var board = sim.Board;
            var item = board.EquipmentItem(plan.Id);
            var source = item.OwnerId == null ? null : board.Person(item.OwnerId);
            if (plan.PersonId == null)
            {
                notes.Add(Loc.T("depoya döner"));
                if (source != null) LosesKit(board, source, item, null, warnings);
                return;
            }
            var target = board.Person(plan.PersonId);
            string slot = PlanningBoardData.EquipmentSlot(item.Kind);
            var old = board.Equipment.FirstOrDefault(e => e.Id != item.Id && e.OwnerId == target.Id && PlanningBoardData.EquipmentSlot(e.Kind) == slot);
            if (old != null)
                notes.Add(Loc.F("yuva dolu: {0} → {1}", PlanningBoardData.EquipmentName(old.Kind), source == null ? Loc.T("DEPO") : source.Name));
            string field = FieldRoleOf(board, target);
            if (slot == "kit" && field != null)
            {
                if (item.Kind == "kit_" + field) notes.Add(Loc.T("saha kiti tamam"));
                else warnings.Add(Loc.F("{0} için {1} gerekiyor.", target.Name, PlanningBoardData.EquipmentName("kit_" + field)));
            }
            if (item.Kind == "emp_cell" && field != "emp") notes.Add(Loc.T("hücre yalnız EMP uzmanında etkili"));
            if (source != null) LosesKit(board, source, item, old, warnings);
        }

        static void ScrapNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var board = sim.Board;
            var item = board.EquipmentItem(plan.Id);
            int metal = board.Material("metal")?.Quantity ?? 0, cap = Math.Max(0, sim.B.boardMaterialsCapacity);
            int gain = Math.Max(0, Math.Min(cap, metal + Math.Max(0, sim.B.boardScrapMetal)) - metal);
            if (gain > 0) notes.Add(Loc.F("kalıcı olarak sökülür: +{0} metal", gain));
            else warnings.Add(Loc.F("metal deposu dolu ({0}): kazanç yok", cap));
            var owner = item.OwnerId == null ? null : board.Person(item.OwnerId);
            if (owner != null) LosesKit(board, owner, item, null, warnings);
        }

        static void StageNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var board = sim.Board;
            int free = sim.BoardMaterialFree(plan.Id);
            bool adding = plan.Amount > BoardDropRules.Staged(board, plan.Id);
            notes.Add(Loc.F("serbest {0} → {1}", free, adding ? free - 1 : free + 1));
            if (adding && Maker(board) == null)
                warnings.Add(Loc.T("üretim cebi boş: kapanışta üretim olmaz"));
        }

        /// <summary>[B] Ayırma: eski hazırlamanın satırları (serbest sayısı; ekleniyorsa ve üretici yoksa uyarı).</summary>
        static void ReserveNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            int free = sim.BoardMaterialFree(plan.Id);
            bool adding = plan.Amount > BoardDropRules.Reserved(sim.Board, plan.Id);
            notes.Add(Loc.F("serbest {0} → {1}", free, adding ? free - 1 : free + 1));
            if (adding && Maker(sim.Board) == null) warnings.Add(Loc.T("üretim cebi boş: kapanışta üretim olmaz"));
        }

        /// <summary>[B] Tarif iş emrine: "girdi: 2 metal + 1 kumaş" ve üretici (yoksa "üretici yok" uyarısı).</summary>
        static void QueueNotes(Simulation sim, BoardDropPlan plan, List<string> notes, List<string> warnings)
        {
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == plan.Id);
            string inputs = InputsLine(recipe);
            if (!string.IsNullOrEmpty(inputs)) notes.Add(Loc.F("girdi: {0}", inputs));
            var maker = Maker(sim.Board);
            if (maker != null) notes.Add(Loc.F("üretici: {0}", maker.Name));
            else warnings.Add(Loc.T("üretici yok"));
        }

        /// <summary>İptalin bedeli: başlamamış emirde ücretsiz, başlamışta harcanan girdi geri gelmez.</summary>
        static string CancelWord(BoardProductionOrder order) =>
            order != null && order.Started ? Loc.T("harcanan girdi geri gelmez") : Loc.T("başlamamış iş · ücretsiz iptal");

        /// <summary>
        /// Tarifin girdileri tek satırda: "2 metal + 1 kumaş" (her girdi <c>"{0} {1}"</c>, ad küçük harf, sıfır adetli girdi
        /// yazılmaz); tarif yoksa null.
        /// </summary>
        public static string InputsLine(BoardRecipe recipe)
        {
            if (recipe == null) return null;
            string line = null;
            foreach (var input in recipe.Inputs)
            {
                if (input == null || input.Quantity <= 0) continue;
                string part = Loc.F("{0} {1}", input.Quantity, DeskText.Lower(PlanningBoardData.MaterialName(input.Kind), DeskFormat.Lang));
                line = line == null ? part : Loc.F("{0} + {1}", line, part);
            }
            return line;
        }

        /// <summary>Kapanışta üretecek kişi (Core <c>AdvanceBoardProduction</c>: çalışabilen ve ÜRETİM cebinde); yoksa null.</summary>
        static ResidentState Maker(PlanningBoardState board) =>
            board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "craft");

        // ---------------------------------------------------------------- ortak

        static string SwapLine(PlanningBoardState board, ResidentState partner, string partnerGoesTo)
        {
            var kit = board.Equipment.FirstOrDefault(e => e.OwnerId == partner.Id && PlanningBoardData.EquipmentSlot(e.Kind) == "kit");
            return kit == null
                ? Loc.F("takas: {0} → {1}", partner.Name, PlaceWord(partnerGoesTo))
                : Loc.F("takas: {0} → {1}, {2} onunla gider", partner.Name, PlaceWord(partnerGoesTo), PlanningBoardData.EquipmentName(kit.Kind));
        }

        /// <summary>
        /// Taşımadan sonra değişen saha ceplerinin (tek emp ana) durumu: boş kalan cep ya da eşleşen kiti olmayan kişi baskın emrini
        /// kapatır (Core <c>PlanningRaidCheck</c>). Eşyalar sahibinde kalır; yalnız görevler yer değiştirir.
        /// </summary>
        static void FieldWarnings(PlanningBoardState board, ResidentState mover, string to, ResidentState partner, string from, List<string> warnings)
        {
            bool noKit = false;
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                ResidentState after;
                if (role == to) after = mover;
                else if (role == from) after = partner;
                else continue;
                if (after == null) warnings.Add(Loc.F("{0} boş kalır: baskın emri verilemez", PlaceWord(role)));
                else if (!HasKit(board, after, role)) noKit = true;
            }
            if (noKit) warnings.Add(Loc.T("KİT YOK: baskın emri verilemez"));
        }

        /// <summary>Eşyayı bırakan kişi saha cebindeyse ve bu onun rol kitiyse (yerine aynı kit gelmiyorsa) uyarı.</summary>
        static void LosesKit(PlanningBoardState board, ResidentState owner, BoardEquipment item, BoardEquipment replacement, List<string> warnings)
        {
            string field = FieldRoleOf(board, owner);
            if (field == null || item.Kind != "kit_" + field) return;
            if (replacement != null && replacement.Kind == item.Kind) return;
            warnings.Add(Loc.F("{0} için {1} gerekiyor.", owner.Name, PlanningBoardData.EquipmentName(item.Kind)));
        }

        static bool HasKit(PlanningBoardState board, ResidentState person, string role) =>
            board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "kit_" + role && e.Charges > 0);

        static bool IsField(string job) => Array.IndexOf(PlanningBoardData.FieldJobs, job) >= 0;

        /// <summary>Kişi bir saha cebindeyse rolü (tek emp ana), değilse null.</summary>
        static string FieldRoleOf(PlanningBoardState board, ResidentState person)
        {
            string place = BoardDropRules.PlaceOf(board, person);
            return IsField(place) ? place : null;
        }

        /// <summary>Kapanışta dinlenmenin getireceği can (Core <c>AdvancePlanningBoard</c>: yatan da dinlenen de iyileşir).</summary>
        static int RestHeal(Simulation sim, ResidentState p)
        {
            int heal = Math.Max(0, sim.B.boardRestHeal);
            int after = p.Down > 0 ? Math.Min(p.MaxHp, Math.Max(1, p.Hp) + heal) : Math.Min(p.MaxHp, p.Hp + heal);
            return Math.Max(0, after - p.Hp);
        }

        /// <summary>Parçaları " · " ile birleştirir (her adım <c>Loc.F</c>).</summary>
        static string Join(List<string> parts)
        {
            string text = null;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                text = text == null ? part : Loc.F("{0} · {1}", text, part);
            }
            return text;
        }
    }
}
