using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Üretimin kapanışta durma nedeni (dilden bağımsız; metni <see cref="ProductionOutlook.StopReason"/>).</summary>
    public enum ProductionStop { None, NoMaker, Starvation, StoreFull, Blackout, NoSupplies }

    /// <summary>
    /// Kapanışta üretimin ne yapacağının tahmini (Revizyon 3 · Aşama 5; PLAN §1.7, D8): hangi tarif, üretici kim, çalışıyor mu, durursa
    /// neden; [B] emirden mi, başlamış mı, kaç vardiya sürer, kaçı bitti, bu kapanıştan sonra kaç biter. Devir fişi
    /// (<c>HandoverSlipFactsBuilder.FillProduction</c>) ve iş emri formu aynı sonucu buradan okur. Saf: durum değiştirmez, RNG çekmez.
    /// </summary>
    public readonly struct ProductionOutlook
    {
        /// <summary>Üretilecek tarifin Core kimliği; üretim yoksa null.</summary>
        public readonly string RecipeId;
        /// <summary>Üreticinin adı (ÜRETİM cebinde çalışan); yoksa null.</summary>
        public readonly string Maker;
        /// <summary>Durma nedeni (çevrilmiş: "üretici yok", "açlık", "eşya deposu dolu"); durmuyorsa null.</summary>
        public readonly string StopReason;
        /// <summary>Kapanışta üretim olur (ilerler ya da biter).</summary>
        public readonly bool Works;
        /// <summary>[B] Tarif oyuncunun iş emrinden (değilse [A] otomatik yol).</summary>
        public readonly bool FromOrder;
        /// <summary>[B] Emir başlamış (girdiler ödendi).</summary>
        public readonly bool Started;
        /// <summary>Tarifin toplam vardiyası, biten vardiya, bu kapanıştan sonra biten (çalışmıyorsa <see cref="Done"/>).</summary>
        public readonly int Turns, Done, After;
        /// <summary>Çıkacak eşyanın türü (tarifin <c>Output</c>'u; bugün tarif kimliğiyle aynı).</summary>
        public readonly string Output;
        /// <summary>Üreticiyi durduran Core koşulu (üretici yok, açlık, eşya deposu dolu); <see cref="StopReason"/>'ın dilsiz karşılığı.</summary>
        public readonly ProductionStop Stop;
        /// <summary>Girdiler hazır: emir başlamış ya da her girdi tam ayrılmış ve stokta; otomatik yolda tarifin girdileri stokta.</summary>
        public readonly bool Ready;
        /// <summary>
        /// Tesis aşama 9 (K19, K24): tesis kipinde emir açıkken bu kapanış eksik rol kiti yapılır, emir ilerlemez (Core
        /// <c>ProduceMissingKit</c>). <see cref="RecipeId"/> ve <see cref="Output"/> kittir; beklemedeki emrin tarifi <see cref="WaitingRecipeId"/>.
        /// </summary>
        public readonly bool KitFirst;
        /// <summary>K19'da bekleyen emrin tarifi; değilse null.</summary>
        public readonly string WaitingRecipeId;

        public ProductionOutlook(string recipeId, string maker, string stopReason, bool works, bool fromOrder, bool started,
            int turns, int done, int after, string output = null, ProductionStop stop = ProductionStop.None, bool ready = false,
            bool kitFirst = false, string waitingRecipeId = null)
        {
            RecipeId = recipeId; Maker = maker; StopReason = stopReason;
            Works = works; FromOrder = fromOrder; Started = started;
            Turns = turns; Done = done; After = after;
            Output = output ?? recipeId; Stop = stop; Ready = ready;
            KitFirst = kitFirst; WaitingRecipeId = waitingRecipeId;
        }

        /// <summary>Üretim yok (tarif yok).</summary>
        public static readonly ProductionOutlook None = default;
        public bool IsNone => RecipeId == null;
        /// <summary>Kapanışta ürün çıkar (ilerleme tarifin süresine ulaşır).</summary>
        public bool Completes => Works && After >= Turns;
    }

    /// <summary>
    /// "Kapanışta üretim olur mu" kuralının tek yeri (PLAN §1.1, W4), Core'un <c>AdvanceBoardProduction</c> koşullarıyla:
    /// <list type="bullet">
    /// <item>üretici: ayakta (<c>Alive &amp;&amp; Down &lt;= 0 &amp;&amp; Hp &gt; 0</c>) ve ÜRETİM cebinde (<c>Job == "craft"</c>) ilk kişi;</item>
    /// <item>eşya deposu: <c>BoardDepotCount &gt;= boardEquipmentCapacity</c> (kişiye bağlı olmayan teçhizat; bağlı eşya tavana sayılmaz);</item>
    /// <item>açlık: kapanış erzağı 0'a iniyorsa (<c>Supplies + BoardFoodPreview &lt;= 0</c>; Core'un kırpılmamış dengesi public değil,
    /// tek fark tam 0 dengesi; devir fişinin kuralı, D8). Kit kilidi: <c>boardStarvingKitProduction</c> açıkken bekleyen tarif eksik bir
    /// rol kitiyse (<c>BoardKitMissing</c>) açlık üretimi durdurmaz;</item>
    /// <item>önce iş emri (başlamış ya da her girdisi tam ayrılmış ve stokta), yoksa otomatik yolun seçeceği tarif (tek kapanış);</item>
    /// <item>hız: <c>BoardSkillTier(üretici, "craft") &gt;= boardCraftSpeedTier</c> ise kapanış başına 1 + <c>boardCraftExtraProgress</c>.</item>
    /// </list>
    /// Durma nedeninin sırası Core'un denetim sırasıdır: karartma (KOS aşama 4; Core üretimi hiç çağırmaz), üretici, depo, açlık.
    /// </summary>
    public static class ProductionForecast
    {
        /// <summary>Simülasyonun şu anki planına göre kapanıştaki üretim; tarif yoksa <see cref="ProductionOutlook.None"/>.</summary>
        public static ProductionOutlook Of(Simulation sim)
        {
            var board = sim?.Board;
            if (board == null) return ProductionOutlook.None;
            var b = sim.B;
            var producer = Producer(board);
            var stop = StopOf(sim, producer, Pending(sim));
            string reason = ReasonText(stop);
            string maker = producer?.Name;
            bool runs = stop == ProductionStop.None;

            var order = board.Orders.FirstOrDefault();
            if (order != null)
            {
                var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
                if (recipe == null) return ProductionOutlook.None;
                // Tesis aşama 9 (K19): Core, açlık denetiminden önce eksik rol kitini yapar; emir bu kapanış ilerlemez
                var kit = MissingKitFirst(sim, producer, stop);
                if (kit != null)
                    return new ProductionOutlook(kit.Id, maker, null, true, false, false, 1, 0, 1, kit.Output, ProductionStop.None, true, true, recipe.Id);
                bool ready = order.Started || recipe.Inputs.All(i =>
                    order.Reserves.Where(r => r.Kind == i.Kind).Sum(r => r.Quantity) == i.Quantity
                    && (board.Material(i.Kind)?.Quantity ?? 0) >= i.Quantity);
                // Tesis aşama 9 (K23): erzak bedeli (köpek) yetmezse başlamamış emir hammadde eksikliği gibi bekler (Core: kapanışın
                // erzak değişiminden sonra Supplies < bedel)
                if (runs && ready && !order.Started && recipe.Supplies > 0 && SuppliesAtProduction(sim) < recipe.Supplies)
                {
                    stop = ProductionStop.NoSupplies;
                    reason = ReasonText(stop);
                    runs = false;
                }
                int turns = recipe.Turns;
                int done = Math.Max(0, turns - order.Remaining);
                bool works = runs && ready;
                int after = done;
                if (works)
                {
                    int step = 1 + (sim.BoardSkillTier(producer, "craft") >= b.boardCraftSpeedTier ? Math.Max(0, b.boardCraftExtraProgress) : 0);
                    after = Math.Max(0, turns - Math.Max(0, order.Remaining - step));
                }
                return new ProductionOutlook(recipe.Id, maker, reason, works, true, order.Started, turns, done, after, recipe.Output, stop, ready);
            }

            if (!board.AutomaticProduction) return ProductionOutlook.None;
            var automatic = sim.AutomaticProductionRecipe;
            if (automatic == null) return ProductionOutlook.None;
            // Otomatik üretim bir kapanışta biter (tarifin süresi yok sayılır): "0/1 → 1/1"
            bool stocked = automatic.Inputs.All(m => (board.Material(m.Kind)?.Quantity ?? 0) >= m.Quantity);
            bool auto = runs && stocked;
            return new ProductionOutlook(automatic.Id, maker, reason, auto, false, false, 1, 0, auto ? 1 : 0, automatic.Output, stop, stocked);
        }

        /// <summary>Kapanışta üretim sırasındaki erzak: bu vardiyanın erzak değişiminden sonra (Core'un <c>AdvancePlanningBoard</c> sırası).</summary>
        public static double SuppliesAtProduction(Simulation sim) => sim?.Shelter == null ? 0
            : Math.Min(sim.B.boardSuppliesCapacity, Math.Max(0, sim.Shelter.Supplies + sim.BoardFoodPreview));

        /// <summary>
        /// Tesis aşama 9 (K19): emir açıkken Core'un <c>ProduceMissingKit</c>'i: karartma yok, üretici var, depo dolu değil;
        /// açlıkta yalnız <c>boardStarvingKitProduction</c> açıkken; <c>FieldJobs</c> sırasıyla ilk eksik rol kitinin tarifi üretim
        /// alanındaki ve serbest hammaddeyle karşılanıyor. Yapılacak kitin tarifi; yoksa null.
        /// </summary>
        public static BoardRecipe MissingKitFirst(Simulation sim, ResidentState producer, ProductionStop stop)
        {
            var board = sim?.Board;
            if (board == null || board.Orders.Count == 0 || producer == null) return null;
            if (stop != ProductionStop.None && stop != ProductionStop.Starvation) return null;
            if (Starving(sim) && !sim.B.boardStarvingKitProduction) return null;
            string kind = PlanningBoardData.FieldJobs.Select(r => "kit_" + r).FirstOrDefault(sim.BoardKitMissing);
            if (kind == null) return null;
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == kind);
            if (recipe == null) return null;
            int Staged(string k) => board.ProductionInputs.Where(m => m.Kind == k).Sum(m => m.Quantity);
            return recipe.Inputs.All(m => Staged(m.Kind) + sim.BoardMaterialFree(m.Kind) >= m.Quantity) ? recipe : null;
        }

        /// <summary>ÜRETİM cebinde ayakta olan ilk kişi (Core'un <c>working &amp;&amp; Job == "craft"</c>'ı); yoksa null.</summary>
        public static ResidentState Producer(PlanningBoardState board) =>
            board?.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "craft");

        /// <summary>Kapanışta sırası gelen tarif: ilk iş emrinin tarifi, emir yoksa otomatik yolun seçeceği; yoksa null.</summary>
        public static BoardRecipe Pending(Simulation sim)
        {
            var board = sim?.Board;
            if (board == null) return null;
            var order = board.Orders.FirstOrDefault();
            if (order != null) return sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
            return board.AutomaticProduction ? sim.AutomaticProductionRecipe : null;
        }

        /// <summary>
        /// Üretimi durduran koşul, Core'un sırasıyla (girdiden bağımsız): üretici, depo, açlık. Açlık, bekleyen tarif
        /// (<paramref name="pending"/>) eksik bir rol kitiyse durdurmaz (kit kilidi).
        /// </summary>
        public static ProductionStop StopOf(Simulation sim, ResidentState producer, BoardRecipe pending = null)
        {
            var board = sim?.Board;
            if (board == null) return ProductionStop.None;
            // KOS aşama 4: karartmada Core üretimi hiç çağırmaz (AdvancePlanningBoard: if (!blackout)); bu yüzden önce gelir
            if (sim.BlackoutActive) return ProductionStop.Blackout;
            if (producer == null) return ProductionStop.NoMaker;
            if (sim.BoardDepotCount >= sim.B.boardEquipmentCapacity) return ProductionStop.StoreFull;
            if (Starving(sim) && !(sim.B.boardStarvingKitProduction && pending != null && sim.BoardKitMissing(pending.Output)))
                return ProductionStop.Starvation;
            return ProductionStop.None;
        }

        /// <summary>Kapanış erzağı 0'a iniyor mu (devir fişinin <c>suppliesAfter &lt;= 0</c> kuralı).</summary>
        public static bool Starving(Simulation sim) =>
            sim?.Shelter != null && sim.Shelter.Supplies + sim.BoardFoodPreview <= 0;

        /// <summary>Durma nedeninin o dildeki sözcüğü; durmuyorsa null.</summary>
        public static string ReasonText(ProductionStop stop)
        {
            switch (stop)
            {
                case ProductionStop.NoMaker: return Loc.T("üretici yok");
                case ProductionStop.Starvation: return Loc.T("açlık");
                case ProductionStop.StoreFull: return Loc.T("eşya deposu dolu");
                case ProductionStop.Blackout: return Loc.T("karartma");
                case ProductionStop.NoSupplies: return Loc.T("erzak yetmiyor · emir bekliyor");
                default: return null;
            }
        }
    }
}
