using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Üretimin kapanışta durma nedeni (dilden bağımsız; metni <see cref="ProductionOutlook.StopReason"/>).</summary>
    public enum ProductionStop { None, NoMaker, Starvation, StoreFull }

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

        public ProductionOutlook(string recipeId, string maker, string stopReason, bool works, bool fromOrder, bool started,
            int turns, int done, int after, string output = null, ProductionStop stop = ProductionStop.None, bool ready = false)
        {
            RecipeId = recipeId; Maker = maker; StopReason = stopReason;
            Works = works; FromOrder = fromOrder; Started = started;
            Turns = turns; Done = done; After = after;
            Output = output ?? recipeId; Stop = stop; Ready = ready;
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
    /// <item>açlık: kapanış erzağı 0'a iniyorsa (<c>Supplies + BoardFoodPreview &lt;= 0</c>; Core'un kırpılmamış dengesi public değil,
    /// tek fark tam 0 dengesi; devir fişinin kuralı, D8);</item>
    /// <item>eşya deposu: <c>Equipment.Count &gt;= boardEquipmentCapacity</c>;</item>
    /// <item>önce iş emri (başlamış ya da her girdisi tam ayrılmış ve stokta), yoksa otomatik yolun seçeceği tarif (tek kapanış);</item>
    /// <item>hız: <c>BoardSkillTier(üretici, "craft") &gt;= boardCraftSpeedTier</c> ise kapanış başına 1 + <c>boardCraftExtraProgress</c>.</item>
    /// </list>
    /// Durma nedeninin sırası Core'un denetim sırasıdır: üretici, açlık, depo.
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
            var stop = StopOf(sim, producer);
            string reason = ReasonText(stop);
            string maker = producer?.Name;
            bool runs = stop == ProductionStop.None;

            var order = board.Orders.FirstOrDefault();
            if (order != null)
            {
                var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
                if (recipe == null) return ProductionOutlook.None;
                bool ready = order.Started || recipe.Inputs.All(i =>
                    order.Reserves.Where(r => r.Kind == i.Kind).Sum(r => r.Quantity) == i.Quantity
                    && (board.Material(i.Kind)?.Quantity ?? 0) >= i.Quantity);
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

        /// <summary>ÜRETİM cebinde ayakta olan ilk kişi (Core'un <c>working &amp;&amp; Job == "craft"</c>'ı); yoksa null.</summary>
        public static ResidentState Producer(PlanningBoardState board) =>
            board?.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "craft");

        /// <summary>Üretimi durduran koşul, Core'un sırasıyla (tarif ve girdiden bağımsız).</summary>
        public static ProductionStop StopOf(Simulation sim, ResidentState producer)
        {
            var board = sim?.Board;
            if (board == null) return ProductionStop.None;
            if (producer == null) return ProductionStop.NoMaker;
            if (Starving(sim)) return ProductionStop.Starvation;
            if (board.Equipment.Count >= sim.B.boardEquipmentCapacity) return ProductionStop.StoreFull;
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
                default: return null;
            }
        }
    }
}
