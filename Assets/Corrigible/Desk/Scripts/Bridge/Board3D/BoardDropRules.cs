using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Bir bırakmanın Core karşılığı: en çok bir eylem (Aşama 5, D3). Eylemlerin hepsi tek komuttur, yalnız <see cref="Replace"/>
    /// tek bırakışta iki kayıt yazar (<c>board_cancel</c> + <c>board_queue</c>, aynı gün, tek <c>ExecuteBoardCommand</c> içinde).
    /// Aşama 5 · [B] sona ekler: <see cref="Queue"/> (<c>board_queue</c>), <see cref="Reserve"/> (<c>board_reserve</c>, mutlak),
    /// <see cref="Cancel"/> (<c>board_cancel</c>), <see cref="Replace"/>.
    /// </summary>
    public enum BoardDropAction { None, Assign, Train, Equip, Scrap, Stage, Queue, Reserve, Cancel, Replace }

    /// <summary>
    /// Çözülmüş bırakma: hangi komut, hangi bağımsız değişkenlerle. <see cref="Id"/>: kişi (Assign, Train), eşya (Equip, Scrap),
    /// hammadde türü (Stage, Reserve) ya da tarif (Queue, Replace); <see cref="Job"/>: görev (Assign), eğitim konusu (Train) ya da
    /// iptal edilecek iş emrinin kimliği (Cancel, Replace; <c>is-N</c>); <see cref="PersonId"/>: eşyanın gideceği kişi (Equip; null =
    /// depo); <see cref="Amount"/>: üretim alanındaki (Stage, <c>board_stage</c>) ya da iş emrine ayrılan (Reserve, <c>board_reserve</c>)
    /// mutlak miktar.
    /// </summary>
    public readonly struct BoardDropPlan
    {
        public readonly BoardDropAction Action;
        public readonly string Id, Job, PersonId;
        public readonly int Amount;

        public BoardDropPlan(BoardDropAction action, string id, string job = null, string personId = null, int amount = 0)
        {
            Action = action; Id = id; Job = job; PersonId = personId; Amount = amount;
        }

        public static readonly BoardDropPlan None = new BoardDropPlan(BoardDropAction.None, null);
        public bool IsNone => Action == BoardDropAction.None;

        /// <summary>Komut (sunucu <c>GameController.ExecuteBoardCommand</c>'a verir); komutsuz bırakmada null.</summary>
        public Func<Simulation, ActResult> Command()
        {
            string id = Id, job = Job, person = PersonId;
            int amount = Amount;
            switch (Action)
            {
                case BoardDropAction.Assign: return s => s.AssignResident(id, job);
                case BoardDropAction.Train: return s => s.TrainResident(id, job);
                case BoardDropAction.Equip: return s => s.EquipResident(id, person);
                case BoardDropAction.Scrap: return s => s.ScrapEquipment(id);
                case BoardDropAction.Stage: return s => s.StageProductionMaterial(id, amount);
                case BoardDropAction.Queue: return s => s.QueueProduction(id);
                case BoardDropAction.Reserve: return s => s.ReserveProductionMaterial(id, amount);
                case BoardDropAction.Cancel: return s => s.CancelProduction(job);
                case BoardDropAction.Replace:
                    // İptal başarılıysa kuyruk reddedilemez (emir sayısı 0, kilit ve baskın iptalde denetlendi; SPB:258–268);
                    // iptal reddedilirse kuyruk denenmez ve iptalin iletisi döner.
                    return s =>
                    {
                        var cancelled = s.CancelProduction(job);
                        return cancelled.Ok ? s.QueueProduction(id) : cancelled;
                    };
                default: return null;
            }
        }
    }

    /// <summary>
    /// Kartlı panonun bırakma kuralları (Revizyon 3 · R15; PLAN §4.7): (kart türü, kimlik, hedef) → tek Core eylemi ya da komutsuz.
    /// Bir bırakma en çok bir eylemdir (<see cref="BoardDropAction"/>); takas tek <c>AssignResident</c>'tır (Core yer değiştirir),
    /// değiştirme (<see cref="BoardDropAction.Replace"/>, Aşama 5 D3) iki kayıttır. Aynı yer, yanlış kart türü ve
    /// pano dışı komutsuzdur (null). Geçerlilik saf denetimlerle bulunur (Core'un kendi denetimleri ve aynı iletiler); hiçbiri durum
    /// değiştirmez, RNG çekmez, komut yazmaz, her üzerine gelmede çağrılabilir. Reddedilen komut kayda girmez.
    /// <para>
    /// Aşama 5 · [B] üretim (PLAN §1.3, §1.8): defterden alınan tarif kopyası (kimlik = tarif) boş iş emrine <c>board_queue</c>,
    /// dolu emre <c>board_cancel</c> + <c>board_queue</c> (aynı tarif: komutsuz); formdaki tarif kartı (kimlik = iş emri) form içinde
    /// yalnız yerleşim, form dışında (<see cref="BoardTargetKind.Book"/>) <c>board_cancel</c>. Stok kartı iş emrine
    /// <c>board_reserve</c> (ayrılan + 1, mutlak); fiş depoya <c>board_reserve</c> (ayrılan − 1). Eski (A dönemi) konmuş fişler
    /// ([<see cref="Staged"/>]) aynı türün fiş sırasında önce gelir ve depoya <c>board_stage</c> (konmuş − 1) ile döner.
    /// </para>
    /// </summary>
    public static class BoardDropRules
    {
        /// <summary>Cep görevleri (BOŞTA dışında): tek emp ana craft cook train.</summary>
        static readonly string[] PocketJobs = { "tek", "emp", "ana", "craft", "cook", "train" };

        static string[] Subjects => PlanningBoardData.Subjects;

        // ---------------------------------------------------------------- sözleşme

        /// <summary>Bırakmanın komutu; komutsuz bırakmada null. EĞİTİM cebinde konu: hedefin satırı, yoksa eğitimdeki kişinin konusu, yoksa "tek".</summary>
        public static Func<Simulation, ActResult> CommandFor(BoardCardKind kind, string id, BoardTarget t, Simulation sim) => CommandFor(kind, id, t, sim, null);

        /// <summary>Bırakmanın komutu; kimse eğitimde değilse EĞİTİM cebinin konusu <see cref="BoardUiState.TrainingTopic"/>.</summary>
        public static Func<Simulation, ActResult> CommandFor(BoardCardKind kind, string id, BoardTarget t, Simulation sim, BoardUiState ui) =>
            Plan(kind, id, t, sim, ui).Command();

        /// <summary>Hedef bu kart için geçerli mi (komut Core'da başarılı olur ya da yalnız yerleşim değişir).</summary>
        public static bool Valid(BoardCardKind kind, string id, BoardTarget t, Simulation sim) => Valid(kind, id, t, sim, null);

        public static bool Valid(BoardCardKind kind, string id, BoardTarget t, Simulation sim, BoardUiState ui)
        {
            if (IsLayoutOnly(kind, id, t, sim)) return true;
            var plan = Plan(kind, id, t, sim, ui);
            return !plan.IsNone && Reason(plan, sim) == null;
        }

        // ---------------------------------------------------------------- ek yardımcılar

        /// <summary>
        /// Komutsuz ama kabul edilen bırakma: iş emrindeki fişin ya da ([B]) formdaki tarif kartının kâğıt üstünde yer değiştirmesi
        /// (yalnız yerleşim dosyası). Sunucu <c>Resolve(true)</c> der; fişte <see cref="BoardUiState.SetChipPose"/>, tarif kartında
        /// <see cref="BoardUiState.RecipePoses"/> ile yeri saklar.
        /// </summary>
        public static bool IsLayoutOnly(BoardCardKind kind, string id, BoardTarget t, Simulation sim)
        {
            if (t.Kind != BoardTargetKind.Order || string.IsNullOrEmpty(id)) return false;
            var board = sim?.Board;
            if (board == null || CommonCheck(sim) != null) return false;
            switch (kind)
            {
                case BoardCardKind.Chip:
                    return BoardUiState.TryParseChipId(id, out var material, out var index) && index < ChipCount(board, material);
                case BoardCardKind.Recipe:
                    return board.Order(id) != null;
                default:
                    return false;
            }
        }

        /// <summary>Komutun Core'da reddedilme nedeni (Core'un iletisi); geçerliyse ya da komut yoksa null.</summary>
        public static string Reason(BoardCardKind kind, string id, BoardTarget t, Simulation sim, BoardUiState ui = null) =>
            Reason(Plan(kind, id, t, sim, ui), sim);

        public static string Reason(BoardDropPlan plan, Simulation sim)
        {
            if (plan.IsNone) return null;
            if (sim == null) return Loc.T("Bu kayıtta vardiya panosu yok.");
            switch (plan.Action)
            {
                case BoardDropAction.Assign: return sim.AssignResidentCheck(plan.Id, plan.Job);
                case BoardDropAction.Train: return TrainCheck(sim, plan.Id, plan.Job);
                case BoardDropAction.Equip: return EquipCheck(sim, plan.Id, plan.PersonId);
                case BoardDropAction.Scrap: return ScrapCheck(sim, plan.Id);
                case BoardDropAction.Stage: return StageCheck(sim, plan.Id, plan.Amount);
                case BoardDropAction.Queue: return QueueCheck(sim, plan.Id, null);
                case BoardDropAction.Reserve: return ReserveCheck(sim, plan.Id, plan.Amount);
                case BoardDropAction.Cancel: return CancelCheck(sim, plan.Job);
                case BoardDropAction.Replace: return CancelCheck(sim, plan.Job) ?? QueueCheck(sim, plan.Id, plan.Job);
                default: return null;
            }
        }

        /// <summary>Bırakmayı çözer (komut kurmadan): önizleme ve sunucu (ör. BOŞTA'ya kabul kayışı) bunu okur.</summary>
        public static BoardDropPlan Plan(BoardCardKind kind, string id, BoardTarget t, Simulation sim, BoardUiState ui = null)
        {
            var board = sim?.Board;
            if (board == null || string.IsNullOrEmpty(id)) return BoardDropPlan.None;
            switch (kind)
            {
                case BoardCardKind.Person: return PersonPlan(board, id, t, sim, ui);
                case BoardCardKind.Item: return ItemPlan(board, id, t);
                case BoardCardKind.Material: return MaterialPlan(board, id, t);
                case BoardCardKind.Chip: return ChipPlan(board, id, t);
                case BoardCardKind.Recipe: return RecipePlan(board, id, t, sim);
                default: return BoardDropPlan.None;
            }
        }

        /// <summary>EĞİTİM cebinin halkalı konusu: eğitimdeki kişinin konusu, yoksa sunumda seçilen konu, yoksa "tek".</summary>
        public static string TrainingSubject(Simulation sim, BoardUiState ui)
        {
            var trainee = sim?.Board?.People.FirstOrDefault(p => p.Alive && p.Job == "train");
            if (trainee != null && IsSubject(trainee.TrainingSubject)) return trainee.TrainingSubject;
            string topic = ui?.TrainingTopic;
            return IsSubject(topic) ? topic : "tek";
        }

        /// <summary>
        /// Konu satırına tıklama: biri eğitimdeyse onu o konuya alır (<c>board_train</c>); kimse yoksa ya da konu aynıysa null (sunucu
        /// yalnız <see cref="BoardUiState.TrainingTopic"/>'i saklar).
        /// </summary>
        public static Func<Simulation, ActResult> TopicCommand(int row, Simulation sim)
        {
            if (row < 0 || row >= Subjects.Length) return null;
            var trainee = sim?.Board?.People.FirstOrDefault(p => p.Alive && p.Job == "train");
            if (trainee == null) return null;
            string id = trainee.Id, subject = Subjects[row];
            if (trainee.TrainingSubject == subject) return null;
            return s => s.TrainResident(id, subject);
        }

        /// <summary>Dosya sekmesi İNCELE (<c>reviewResident</c>).</summary>
        public static Func<Simulation, ActResult> ReviewCommand(string personId)
        {
            if (string.IsNullOrEmpty(personId)) return null;
            return s => s.ReviewResident(personId);
        }

        /// <summary>Dosya sekmesi ERİŞİM: kısıtlı değilse kısıtlar, kısıtlıysa serbest bırakır (<c>restrictResident</c> "1"/"0").</summary>
        public static Func<Simulation, ActResult> AccessCommand(string personId, Simulation sim)
        {
            var person = sim?.Board?.Person(personId);
            if (person == null) return null;
            bool restrict = !person.Restricted;
            return s => s.RestrictResident(personId, restrict);
        }

        /// <summary>Dosya sekmesi TAHLİYE (<c>dismissResident</c>; basılı tutma görünümde).</summary>
        public static Func<Simulation, ActResult> DismissCommand(string personId)
        {
            if (string.IsNullOrEmpty(personId)) return null;
            return s => s.DismissResident(personId);
        }

        /// <summary>Kişi bir cepte (BOŞTA dışı) mı: tek emp ana craft cook train.</summary>
        public static bool IsPocketJob(string job) => Array.IndexOf(PocketJobs, job) >= 0;

        /// <summary>Cepteki canlı kişi (Core takası yüzünden en çok bir); boşsa null.</summary>
        public static ResidentState Occupant(PlanningBoardState board, string job) =>
            board == null || !IsPocketJob(job) ? null : board.People.FirstOrDefault(p => p.Alive && p.Job == job);

        /// <summary>Kişinin panodaki yeri: cebin görevi ya da "rest" (BOŞTA destesi; emekli görevler dahil).</summary>
        public static string PlaceOf(PlanningBoardState board, ResidentState person)
        {
            if (person == null || !IsPocketJob(person.Job)) return "rest";
            return Occupant(board, person.Job) == person ? person.Job : "rest";
        }

        /// <summary>Üretim alanındaki miktar (Core'un <c>StageProductionMaterial</c>'ı ilk kaydı okur).</summary>
        public static int Staged(PlanningBoardState board, string kind) =>
            board?.ProductionInputs.FirstOrDefault(m => m.Kind == kind)?.Quantity ?? 0;

        // ---------------------------------------------------------------- Aşama 5 (PLAN §1.8, §1.9)

        /// <summary>
        /// Kart alınabilir mi (sürükleme denemesinde; tık seçimdir, denetlenmez): null = alınır; metin = ret nedeni, Core'un iletisi
        /// ve Core'un sırasıyla (oyun bitti, pano yok, baskın, kilit). Stok kartında serbest 0 ise "Serbest hammadde yetersiz."
        /// (Core <c>ReserveProductionMaterial</c>'ın iletisi): kart kalkmaz, titrer ve serbest sayısı kırmızı yanar. Serbest &gt; 0
        /// ama iş emri yoksa ya da tarif bu türü istemiyorsa kart kalkar; bırakış Core iletisiyle geri döner (bu yetersiz kaynak değil).
        /// </summary>
        public static string CanPick(BoardCardKind kind, string id, Simulation sim)
        {
            if (sim == null) return Loc.T("Bu kayıtta vardiya panosu yok.");
            string why = CommonCheck(sim);
            if (why != null) return why;
            if (kind == BoardCardKind.Material && Array.IndexOf(PlanningBoardData.MaterialKinds, id) >= 0 && sim.BoardMaterialFree(id) <= 0)
                return Loc.T("Serbest hammadde yetersiz.");
            return null;
        }

        /// <summary>
        /// İş emrinde çizilen fiş sayısının tek kaynağı: eski (A dönemi) konmuş fiş (<see cref="Staged"/>) + [B] başlamamış emrin bu
        /// türden ayrılanı (<see cref="Reserved"/>). Fiş sırası: önce konmuş fişler (0 … konmuş − 1), sonra ayrılanlar; yeni ayrılan
        /// fişin sırası bırakmadan önceki <see cref="ChipCount"/>'tır. Başlamış emirde ayrılan fiş yok (girdiler ödendi, D7).
        /// </summary>
        public static int ChipCount(PlanningBoardState board, string kind) => Staged(board, kind) + Reserved(board, kind);

        /// <summary>
        /// [B] Başlamamış iş emrinin (Core tek emir kuralı: <c>Orders[0]</c>; <c>ReserveProductionMaterial</c> onu okur) bu türden
        /// ayrılan miktarı; emir yoksa ya da başlamışsa 0.
        /// </summary>
        public static int Reserved(PlanningBoardState board, string kind)
        {
            var order = board?.Orders.FirstOrDefault();
            if (order == null || order.Started) return 0;
            return order.Reserves.Where(m => m.Kind == kind).Sum(m => m.Quantity);
        }

        /// <summary>Tarif kartının tarifi: kimlik bir iş emriyse (formdaki kart) onun tarifi, değilse tarif kimliğinin kendisi.</summary>
        public static BoardRecipe RecipeOf(Simulation sim, string cardId)
        {
            if (sim?.Board == null || string.IsNullOrEmpty(cardId)) return null;
            var order = sim.Board.Order(cardId);
            string recipeId = order != null ? order.RecipeId : cardId;
            return sim.BoardRecipes.FirstOrDefault(r => r.Id == recipeId);
        }

        // ---------------------------------------------------------------- çözüm

        static BoardDropPlan PersonPlan(PlanningBoardState board, string id, BoardTarget t, Simulation sim, BoardUiState ui)
        {
            var person = board.Person(id);
            if (person == null) return BoardDropPlan.None;
            switch (t.Kind)
            {
                case BoardTargetKind.Person:
                {
                    // Başka bir kişi kartının üstü = o kişinin yeri (takas Core'da)
                    var other = board.Person(t.Id);
                    if (other == null || other.Id == id || !other.Alive) return BoardDropPlan.None;
                    string place = PlaceOf(board, other);
                    return place == "rest" ? ToRest(person) : ToPocket(person, place, -1, sim, ui);
                }
                case BoardTargetKind.Bosta:
                case BoardTargetKind.Deck:
                    return ToRest(person);
                case BoardTargetKind.Pocket:
                    return t.Job == "rest" ? ToRest(person) : ToPocket(person, t.Job, t.Row, sim, ui);
                case BoardTargetKind.Topic:
                {
                    if (t.Row < 0 || t.Row >= Subjects.Length) return BoardDropPlan.None;
                    string subject = Subjects[t.Row];
                    if (person.Job == "train" && person.TrainingSubject == subject) return BoardDropPlan.None;
                    return new BoardDropPlan(BoardDropAction.Train, id, subject);
                }
                default:
                    return BoardDropPlan.None;
            }
        }

        /// <summary>BOŞTA: zaten destede olan (dinlenen ya da emekli görevli) için aynı yer.</summary>
        static BoardDropPlan ToRest(ResidentState person) =>
            IsPocketJob(person.Job) ? new BoardDropPlan(BoardDropAction.Assign, person.Id, "rest") : BoardDropPlan.None;

        static BoardDropPlan ToPocket(ResidentState person, string job, int row, Simulation sim, BoardUiState ui)
        {
            if (!IsPocketJob(job) || person.Job == job) return BoardDropPlan.None;
            if (job != "train") return new BoardDropPlan(BoardDropAction.Assign, person.Id, job);
            string subject = row >= 0 && row < Subjects.Length ? Subjects[row] : TrainingSubject(sim, ui);
            return new BoardDropPlan(BoardDropAction.Train, person.Id, subject);
        }

        static BoardDropPlan ItemPlan(PlanningBoardState board, string id, BoardTarget t)
        {
            var item = board.EquipmentItem(id);
            if (item == null) return BoardDropPlan.None;
            switch (t.Kind)
            {
                case BoardTargetKind.Person:
                {
                    var target = board.Person(t.Id);
                    if (target == null || item.OwnerId == target.Id) return BoardDropPlan.None;
                    return new BoardDropPlan(BoardDropAction.Equip, id, null, target.Id);
                }
                case BoardTargetKind.Pocket:
                {
                    // Cep dikdörtgeni, içindeki kişi kartı sayılır; boş cebe eşya bırakılmaz
                    var occupant = Occupant(board, t.Job);
                    if (occupant == null || item.OwnerId == occupant.Id) return BoardDropPlan.None;
                    return new BoardDropPlan(BoardDropAction.Equip, id, null, occupant.Id);
                }
                case BoardTargetKind.Store:
                    return item.OwnerId == null ? BoardDropPlan.None : new BoardDropPlan(BoardDropAction.Equip, id);
                case BoardTargetKind.Hurda:
                    return new BoardDropPlan(BoardDropAction.Scrap, id);
                default:
                    return BoardDropPlan.None;
            }
        }

        /// <summary>[B] Stok kartı iş emrine: bu türün ayrılanı + 1 (mutlak). Emir yoksa ya da tarif istemiyorsa Core iletisiyle reddedilir.</summary>
        static BoardDropPlan MaterialPlan(PlanningBoardState board, string kind, BoardTarget t)
        {
            if (t.Kind != BoardTargetKind.Order || Array.IndexOf(PlanningBoardData.MaterialKinds, kind) < 0) return BoardDropPlan.None;
            return new BoardDropPlan(BoardDropAction.Reserve, kind, null, null, Reserved(board, kind) + 1);
        }

        /// <summary>
        /// Fiş depoya: sıra konmuş fişlerdeyse eski yol (<c>board_stage</c> konmuş − 1), ayrılanlardaysa <c>board_reserve</c> (ayrılan − 1).
        /// </summary>
        static BoardDropPlan ChipPlan(PlanningBoardState board, string chipId, BoardTarget t)
        {
            if (!BoardUiState.TryParseChipId(chipId, out var kind, out var index)) return BoardDropPlan.None;
            int staged = Staged(board, kind), reserved = Reserved(board, kind);
            if (index >= staged + reserved || t.Kind != BoardTargetKind.Store) return BoardDropPlan.None;
            return index < staged
                ? new BoardDropPlan(BoardDropAction.Stage, kind, null, null, staged - 1)
                : new BoardDropPlan(BoardDropAction.Reserve, kind, null, null, reserved - 1);
        }

        /// <summary>
        /// [B] Tarif kartı. Formdaki kart (kimlik = iş emri): deftere (form dışı) iptal; form içi yalnız yerleşimdir
        /// (<see cref="IsLayoutOnly"/>), başka hedef komutsuz. Defterden kopya (kimlik = tarif): iş emrine kuyruk; emir doluysa
        /// değiştirme (iptal + kuyruk); aynı tarifin emri varsa aynı yer (komutsuz; başlamış işin girdisi boşa harcanmasın). Kopyanın
        /// deftere dönüşü komutsuzdur.
        /// </summary>
        static BoardDropPlan RecipePlan(PlanningBoardState board, string id, BoardTarget t, Simulation sim)
        {
            var own = board.Order(id);
            if (own != null)
                return t.Kind == BoardTargetKind.Book ? new BoardDropPlan(BoardDropAction.Cancel, own.RecipeId, own.Id) : BoardDropPlan.None;
            if (t.Kind != BoardTargetKind.Order || !sim.BoardRecipes.Any(r => r.Id == id)) return BoardDropPlan.None;
            var current = board.Orders.FirstOrDefault();
            if (current == null) return new BoardDropPlan(BoardDropAction.Queue, id);
            if (current.RecipeId == id) return BoardDropPlan.None;
            return new BoardDropPlan(BoardDropAction.Replace, id, current.Id);
        }

        // ---------------------------------------------------------------- saf denetimler (Core'un sırası ve iletileri)

        static bool IsSubject(string subject) => subject != null && Array.IndexOf(PlanningBoardData.Subjects, subject) >= 0;

        /// <summary>Core <c>BoardCommandCheck</c> ile aynı sıra.</summary>
        static string CommonCheck(Simulation sim)
        {
            if (sim.S.Ending != null) return Loc.T("Oyun bitti.");
            if (sim.Board == null) return Loc.T("Bu kayıtta vardiya panosu yok.");
            if (sim.S.Raid != null) return Loc.T("Baskın sürerken plan değiştirilemez.");
            if (sim.Board.Locked) return Loc.T("Plan kilitli. Fişi devir kutusuna bırak.");
            return null;
        }

        /// <summary>Core <c>BoardResidentJobCheck</c> ile aynı sıra.</summary>
        static string JobCheck(Simulation sim, ResidentState person, string job)
        {
            if (person == null || !person.Alive) return Loc.T("Bu sakin görev alamıyor.");
            if (Array.IndexOf(PlanningBoardData.Jobs, job) < 0) return Loc.T("Böyle bir görev yok.");
            if (job == "rest") return null;
            if (person.Down > 0 || person.Hp <= 0) return Loc.T("Yatan yaralı önce dinlenmeli.");
            if (person.Restricted)
            {
                if (Array.IndexOf(PlanningBoardData.FieldJobs, job) >= 0 || job == "craft" || job == "contact" || job == "watch")
                    return Loc.T("Kısıtlı sakin bu göreve erişemez.");
                if (!sim.CanRestrictedResidentWork(person.Id)) return Loc.T("Kısıtlı sakin için ayrı gözetmen gerekiyor.");
            }
            return null;
        }

        /// <summary><c>TrainResident</c>'ın denetimleri (Core'da ayrı bir denetim yöntemi yok).</summary>
        static string TrainCheck(Simulation sim, string personId, string subject)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            var person = sim.Board.Person(personId);
            why = JobCheck(sim, person, "train");
            if (why != null) return why;
            if (!IsSubject(subject)) return Loc.T("Böyle bir eğitim alanı yok.");
            if (person.Skill(subject) >= sim.B.boardExpertAt) return Loc.T("Kişi bu alanda zaten uzman.");
            if (person.Job == "train" && person.TrainingSubject == subject) return Loc.T("Bu eğitim zaten seçili.");
            var other = sim.Board.People.FirstOrDefault(p => p.Alive && p.Id != personId && p.Job == "train");
            return other == null ? null : JobCheck(sim, other, person.Job);
        }

        /// <summary><c>EquipResident</c>'ın denetimleri.</summary>
        static string EquipCheck(Simulation sim, string equipmentId, string personId)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            var equipment = sim.Board.EquipmentItem(equipmentId);
            if (equipment == null || equipment.Charges <= 0) return Loc.T("Bu eşya depoda veya kişide bulunmuyor.");
            personId = string.IsNullOrEmpty(personId) ? null : personId;
            var person = personId == null ? null : sim.Board.Person(personId);
            if (personId != null && (person == null || !person.Alive || person.Restricted)) return Loc.T("Bu sakin teçhizat kullanamıyor.");
            if (equipment.OwnerId == personId) return Loc.T("Eşya zaten bu cepte.");
            return null;
        }

        /// <summary><c>ScrapEquipment</c>'ın denetimleri.</summary>
        static string ScrapCheck(Simulation sim, string equipmentId)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            return sim.Board.EquipmentItem(equipmentId) == null ? Loc.T("Hurdaya ayrılabilen bir eşya seç.") : null;
        }

        /// <summary>
        /// <c>QueueProduction</c>'ın denetimleri. <paramref name="cancelledOrderId"/> doluysa (değiştirme) o emir iptal edilmiş sayılır:
        /// iptal başarılıysa kuyruk yalnız bilinmeyen tarifte reddedilebilir, o da plan kurulurken elenir.
        /// </summary>
        static string QueueCheck(Simulation sim, string recipeId, string cancelledOrderId)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            int orders = sim.Board.Orders.Count(o => cancelledOrderId == null || o.Id != cancelledOrderId);
            if (orders != 0) return Loc.T("Önce mevcut üretim emrini tamamla veya iptal et.");
            return sim.BoardRecipes.Any(r => r.Id == recipeId) ? null : Loc.T("Böyle bir tarif yok.");
        }

        /// <summary><c>ReserveProductionMaterial</c>'ın denetimleri (Core'un sırası ve iletileri).</summary>
        static string ReserveCheck(Simulation sim, string kind, int amount)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            var order = sim.Board.Orders.FirstOrDefault();
            if (order == null || order.Started) return Loc.T("Girdisi ayrılabilen bir taslak iş yok.");
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
            int need = recipe?.Inputs.FirstOrDefault(i => i.Kind == kind)?.Quantity ?? 0;
            int old = order.Reserves.FirstOrDefault(i => i.Kind == kind)?.Quantity ?? 0;
            if (amount < 0 || amount > need || need == 0) return Loc.T("Bu tarif için uygun tür ve miktarı ayır.");
            if (amount == old) return Loc.T("Bu miktar zaten ayrılmış.");
            if (amount > sim.BoardMaterialFree(kind) + old) return Loc.T("Serbest hammadde yetersiz.");
            return null;
        }

        /// <summary><c>CancelProduction</c>'ın denetimleri.</summary>
        static string CancelCheck(Simulation sim, string orderId)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            return sim.Board.Order(orderId) == null ? Loc.T("Böyle bir iş emri yok.") : null;
        }

        /// <summary><c>StageProductionMaterial</c>'ın denetimleri.</summary>
        static string StageCheck(Simulation sim, string kind, int amount)
        {
            string why = CommonCheck(sim);
            if (why != null) return why;
            var board = sim.Board;
            if (!board.AutomaticProduction || Array.IndexOf(PlanningBoardData.MaterialKinds, kind) < 0 || amount < 0)
                return Loc.T("Geçerli hammadde miktarı seç.");
            int old = Staged(board, kind);
            if (amount == old || amount > sim.BoardMaterialFree(kind) + old) return Loc.T("Serbest hammadde yetersiz veya miktar aynı.");
            return null;
        }
    }
}
