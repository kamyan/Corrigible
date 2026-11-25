using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Tests
{
    /// <summary>
    /// Denge ölçümünün vardiya politikası (planlama botu): Camp8'in (Tools/DengeBotu) ve <see cref="FacilityBotTests"/>'in ortak
    /// planı (tesis aşama 8, K29; önce iki elle eşitlenen kopyaydı). Yuvasız görevleri Boşta'ya indirir (<see cref="Retire"/>),
    /// otomatik üretimi açar, eksik rol kitinin malzemesini üretim alanına koyar, kit sahibini saha rolüne alır, üreticiyi ve aşçıyı
    /// doldurur. Ayrıca sunucu ödülünü seçer (<see cref="Odul"/>), açılan ekipman tariflerinden iş emri açar (<see cref="Arac"/>) ve
    /// saha rollerine ekipman takar (tesis tek baskın kipi, kayıt 21). Yalnız komut verir; durumu okur, yazmaz.
    /// Aşama 11 (kuşatma): karar vardiyasında savun ya da bırak (<see cref="Savun"/>, <see cref="Siege"/>); savunma kapalıyken kuşatma
    /// olmadığı için komut verilmez.
    /// </summary>
    public sealed class CampaignBot
    {
        /// <summary>Sunumun kuralı (Camp8 RETIRE=1): yayındaki panoda yuvası olmayan görevler (care, gather, watch, contact) Boşta'ya
        /// iner, bot onlara kimseyi atamaz.</summary>
        public bool Retire = true;
        /// <summary>Camp6 davranışı (Camp8 OLDBOT=1): yalnız depodaki kit devredilir, eksik kitte toplayıcı üretime alınmaz.</summary>
        public bool OldBot;
        /// <summary>Sunucu ödülü: "tarif" (hep tarif; cevap paketinde de, K22: kazanımsız kalır), "kazanim" ya da "onlem" (aşama 10,
        /// K31: temel pakette tarif; cevap paketinde tarif yalnız cevabın sınıfında önlem kademesi ≥ 1 ve temel tarifi bu harekâtta
        /// açıkken, Söküm'ün temeli Teknisyen kiti hep açık; öteki durumda kazanım).</summary>
        public string Odul = FacilityGear.ChoiceRecipe;
        /// <summary>Aşama 10 (Camp8 TAK=hic, K32): false iken üretilen ekipman saha rollerine takılmaz (üretim yolunun sınaması:
        /// emir, hammadde ve depo aynı, kadro boş gider).</summary>
        public bool TakeGear = true;
        /// <summary>Ekipman üretimi: "yok" (emir yok), "tek" (ilk açılan tür; saha rolü başına bir), "donus" (stokta en az olan tür,
        /// eşitlikte bu harekâtta en az emri açılan; toplam stok hedefi <see cref="StockTarget"/>; saha rollerine dönüşümlü farklı türler).</summary>
        public string Arac = "donus";
        /// <summary>Ekipman stoğu hedefi (depo + kişiler).</summary>
        public int StockTarget = 3;
        /// <summary>Başlamayan (girdisi ödenmeyen: üretici yok, hammadde başka işe gitti, köpekte erzak yetmedi) taslak emrin iptal
        /// edildiği vardiya (kit hammaddesini kilitlemesin).</summary>
        public int DraftShifts = 3;
        /// <summary>Aşama 11, kuşatma kararı (Camp8 SAVUN): "hep" karar vardiyasında savunur (savunma reddedilirse, ör. hazır kadro yoksa,
        /// bırakır: <see cref="DefendRefused"/>), "hic" açıkça bırakır, "yok" komut vermez (kanat kapanışta kendiliğinden bırakılır).</summary>
        public string Savun = "hep";
        /// <summary>Aşama 11 sayaçları: savunma emri, komutla bırakma ve savunma reddedildiği için bırakma (C satırı defNoSquad).</summary>
        public int Defended, Yielded, DefendRefused;

        /// <summary>Sayaçlar (Camp8 C satırı): açılan emir, iptal edilen taslak, seçilen ödüller (cevap tarifi ayrıca:
        /// <see cref="RewardAnswer"/>, <see cref="RewardRecipe"/>'nin alt kümesi).</summary>
        public int Orders, DraftsCancelled, RewardRecipe, RewardPerk, RewardAnswer;
        string draftId;
        int draftDay;
        readonly Dictionary<string, int> ordered = new Dictionary<string, int>();
        /// <summary>Bu harekâtta türün açılan emir sayısı ("donus"un eşitlik kuralı).</summary>
        int Made(string kind) => ordered.TryGetValue(kind, out int n) ? n : 0;

        /// <summary>
        /// Aşama 11: kuşatma kararı. Karar vardiyası değilse (<see cref="Simulation.SiegeDecisionDue"/>; kuşatma bu vardiya kuruldu ya da
        /// yok) ya da "yok"ta null; "hic"te "yield"; "hep"te savunma verilebiliyorsa (<see cref="Simulation.DefendSectorCheck"/>) "defend",
        /// değilse "yield". Kural Core okuyucularından gelir, kopyalanmaz.
        /// </summary>
        public static string SiegeChoice(Simulation sim, string savun)
        {
            if (savun == "yok" || !sim.SiegeDecisionDue) return null;
            if (savun == "hic") return "yield";
            return sim.DefendSectorCheck() == null ? "defend" : "yield";
        }

        /// <summary>
        /// Aşama 11: kuşatma kararını komutla verir (<see cref="SiegeChoice"/>; savunma tahtasını çağıran oynar). Savunma emri verildiyse
        /// true. Reddedilen komut istisna atar.
        /// </summary>
        public bool Siege(Simulation sim)
        {
            string choice = SiegeChoice(sim, Savun);
            if (choice == null) return false;
            if (choice == "defend")
            {
                var d = sim.DefendSector();
                if (!d.Ok) throw new InvalidOperationException("savun: " + d.Msg);
                Defended++;
                return true;
            }
            if (Savun == "hep") DefendRefused++;
            var y = sim.YieldSector();
            if (!y.Ok) throw new InvalidOperationException("bırak: " + y.Msg);
            Yielded++;
            return false;
        }

        public static bool Fit(Simulation sim, ResidentState p) => p.Alive && p.Down <= 0 && p.Hp > 0 && !p.Restricted && !p.Isolated
            && sim.S.Day >= (sim.Admission.File(p.Id)?.IdleUntil ?? -1);

        /// <summary>
        /// Bekleyen sunucu ödülünü seçer (baskın emrini kilitler; baskın sonucundan hemen sonra ve vardiya başında çağrılır). Ödülü kuran
        /// baskın oyunu da bitirebilir (dağılma güveni ödülden sonra düşer): biten oyunda seçim yok. Reddedilirse istisna.
        /// </summary>
        public void ChooseReward(Simulation sim)
        {
            string region = sim.Facility?.PendingReward;
            if (string.IsNullOrEmpty(region) || sim.S.Ending != null) return;
            string choice = Odul == "onlem" ? AnswerChoice(sim, region) : Odul;
            var r = sim.ChooseServerReward(region, choice);
            if (!r.Ok) throw new InvalidOperationException("ödül " + region + " " + choice + ": " + r.Msg);
            if (choice == FacilityGear.ChoicePerk) RewardPerk++;
            else
            {
                RewardRecipe++;
                if (FacilityGear.IsAnswer(FacilityGear.Reward(region).Recipe)) RewardAnswer++;
            }
        }

        /// <summary>
        /// "onlem" politikasının seçimi (K31; §13 "Boş cevap tarifi"nin bot karşılığı): temel pakette tarif; cevap paketinde tarif yalnız
        /// cevabın sınıfında yürürlükte önlem varken (<see cref="Simulation.CounterLevel"/> ≥ 1; ödül baskın sonucundan sonra seçildiği
        /// için o baskının öğrenmesi görülür) ve cevabın temel tariflerinden biri bu harekâtta açıkken (Söküm'ün temeli Teknisyen kiti
        /// hep açık); değilse kazanım.
        /// </summary>
        public static string AnswerChoice(Simulation sim, string region)
        {
            string recipe = FacilityGear.Reward(region).Recipe;
            if (!FacilityGear.IsAnswer(recipe)) return FacilityGear.ChoiceRecipe;
            string cls = FacilityCounters.ClassOfAnswer(recipe);
            bool baseOpen = FacilityGear.AnswerBases(recipe).Any(b => b == "kit_tek" || sim.Facility.Recipes.Contains(b));
            return sim.CounterLevel(cls) >= 1 && baseOpen ? FacilityGear.ChoiceRecipe : FacilityGear.ChoicePerk;
        }

        public void Plan(Simulation sim)
        {
            var board = sim.Board;
            if (Retire)
                foreach (var rp in board.People.Where(x => x.Alive && (x.Job == "care" || x.Job == "gather" || x.Job == "watch" || x.Job == "contact")).ToList())
                    sim.AssignResident(rp.Id, "rest");
            if (!board.AutomaticProduction) sim.EnableAutomaticProduction();
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                if (board.Equipment.Any(e => e.Kind == "kit_" + role && e.Charges > 0)) continue;
                var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == "kit_" + role);
                if (recipe == null) continue;
                foreach (var m in recipe.Inputs)
                {
                    int have = board.ProductionInputs.Where(i => i.Kind == m.Kind).Sum(i => i.Quantity);
                    if (have < m.Quantity) sim.StageProductionMaterial(m.Kind, m.Quantity);
                }
                break;
            }
            QueueGear(sim);
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                var holder = board.People.FirstOrDefault(p => Fit(sim, p) && board.Equipment.Any(e => e.OwnerId == p.Id && e.Kind == "kit_" + role && e.Charges > 0));
                if (holder == null)
                {
                    var kit = board.Equipment.FirstOrDefault(e => e.Kind == "kit_" + role && e.Charges > 0 && e.OwnerId == null);
                    // Yatan/uygun olmayan kişinin devredilebilir kiti de alınır (OldBot: Camp6 davranışı, yalnız depodaki kit)
                    if (kit == null && !OldBot)
                        kit = board.Equipment.FirstOrDefault(e => e.Kind == "kit_" + role && e.Charges > 0 && !e.Bound && e.OwnerId != null
                            && board.Person(e.OwnerId) is var ow && ow != null && !Fit(sim, ow));
                    var cand = board.People.Where(p => Fit(sim, p) && !PlanningBoardData.FieldJobs.Contains(p.Job)
                            && !board.Equipment.Any(e => e.OwnerId == p.Id && e.Bound && PlanningBoardData.EquipmentSlot(e.Kind) == "kit"))
                        .OrderBy(p => OldBot || !board.Equipment.Any(e => e.OwnerId == p.Id && PlanningBoardData.EquipmentSlot(e.Kind) == "kit") ? 0 : 1)
                        .ThenByDescending(p => p.Skill(role)).FirstOrDefault();
                    if (kit != null && cand != null && sim.EquipResident(kit.Id, cand.Id).Ok) holder = cand;
                }
                if (holder != null && holder.Job != role) sim.AssignResident(holder.Id, role);
            }
            EquipGear(sim);
            foreach (string job in Retire ? new[] { "craft", "cook" } : new[] { "craft", "cook", "gather" })
            {
                if (board.People.Any(p => p.Alive && p.Job == job)) continue;
                var p2 = board.People.Where(p => Fit(sim, p) && p.Job == "rest").OrderByDescending(p => p.Skill(job)).FirstOrDefault();
                // Eksik rol kiti varken üretici yoksa toplayıcı ya da bakıcı üretime alınır (OldBot'ta yok)
                if (p2 == null && job == "craft" && !OldBot && PlanningBoardData.FieldJobs.Any(r => !board.Equipment.Any(e => e.Kind == "kit_" + r && e.Charges > 0)))
                    p2 = board.People.Where(p => Fit(sim, p) && (p.Job == "gather" || p.Job == "care")).OrderByDescending(p => p.Skill(job)).FirstOrDefault();
                if (p2 != null) sim.AssignResident(p2.Id, job);
            }
        }

        /// <summary>Açılan temel ekipman tarifleri, açılış sırasıyla.</summary>
        public static List<BoardRecipe> GearRecipes(Simulation sim) =>
            sim.BoardRecipes.Where(r => r.Reward && FacilityGear.IsGear(r.Output)).ToList();

        /// <summary>Panodaki ekipman stoğu (depo + kişiler; dinlenen köpek dahil).</summary>
        static int Stock(Simulation sim, string kind) => sim.Board.Equipment.Count(e => e.Kind == kind && e.Charges > 0);

        /// <summary>
        /// İş emri (tesis kipi, <see cref="Arac"/> "tek" ya da "donus"): emir yoksa, hiçbir rol kiti eksik değilken, depoda yer varken ve
        /// bütün girdiler serbest hammaddeden karşılanıyorken stok hedefinin altındaki türün emrini açar ve girdilerini tam ayırır
        /// (eksik ayrılmış taslak fişi ve baskın emrini kilitler: <c>BoardPlanCheck</c> "adedi … olmalı"). Köpek yalnız erzak bedeli ve üç
        /// vardiyalık tüketim payı kadar erzak varken. Taslak <see cref="DraftShifts"/> vardiyada başlamazsa, girdisi eksik kalmışsa ya
        /// da emir açıkken depo dolduysa (fiş kilitlenirdi) iptal edilir.
        /// </summary>
        public void QueueGear(Simulation sim)
        {
            if (Arac == "yok" || sim.Board == null) return;
            var board = sim.Board;
            var open = GearRecipes(sim);
            if (open.Count == 0) return;
            var order = board.Orders.FirstOrDefault();
            if (order != null)
            {
                bool full = sim.BoardDepotCount >= sim.B.boardEquipmentCapacity;
                if (!full && (order.Started || order.Id != draftId)) return;
                if (full || sim.S.Day - draftDay >= DraftShifts || !Reserved(sim, order))
                {
                    if (sim.CancelProduction(order.Id).Ok) DraftsCancelled++;
                    draftId = null;
                }
                return;
            }
            if (PlanningBoardData.FieldJobs.Any(r => sim.BoardKitMissing("kit_" + r))) return;
            if (sim.BoardDepotCount >= sim.B.boardEquipmentCapacity) return;
            bool fed(BoardRecipe r) => (r.Supplies <= 0 || sim.Shelter.Supplies >= r.Supplies + 3 * sim.BoardConsumption)
                && r.Inputs.All(m => sim.BoardMaterialFree(m.Kind) >= m.Quantity);
            BoardRecipe pick = null;
            if (Arac == "tek")
            {
                var first = open[0];
                if (Stock(sim, first.Output) < PlanningBoardData.FieldJobs.Length && fed(first)) pick = first;
            }
            else
            {
                int total = open.Sum(r => Stock(sim, r.Output));
                // Stokta en az olan tür; eşitlikte bu harekâtta en az emri açılan (yoksa ilk açılan tür hep önde kalırdı: ölenle silinen
                // eşya sık), sonra açılış sırası
                if (total < StockTarget)
                    pick = open.Where(fed).Select((r, i) => (r, i)).OrderBy(x => Stock(sim, x.r.Output)).ThenBy(x => Made(x.r.Output)).ThenBy(x => x.i)
                        .Select(x => x.r).FirstOrDefault();
            }
            if (pick == null || !sim.QueueProduction(pick.Id).Ok) return;
            Orders++;
            ordered[pick.Output] = Made(pick.Output) + 1;
            order = board.Orders.First();
            draftId = order.Id;
            draftDay = sim.S.Day;
            var recipe = sim.BoardRecipes.First(r => r.Id == order.RecipeId);
            foreach (var input in recipe.Inputs) sim.ReserveProductionMaterial(input.Kind, input.Quantity);
            if (!Reserved(sim, order) && sim.CancelProduction(order.Id).Ok) { DraftsCancelled++; draftId = null; }
        }

        /// <summary>Taslak emrin bütün girdileri tarifin adediyle ayrılmış mı (<c>BoardPlanCheck</c>'in kuralı).</summary>
        static bool Reserved(Simulation sim, BoardProductionOrder order)
        {
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
            return recipe != null && recipe.Inputs.All(i => order.Reserves.Where(r => r.Kind == i.Kind).Sum(r => r.Quantity) == i.Quantity);
        }

        /// <summary>
        /// Takma (tesis kipi): her saha rolü sahibinin yardımcı yuvasına istenen ekipman. "tek": hepsine ilk açılan tür; "donus": stokta
        /// kullanılabilir eşyası olan türler, baskın sayısıyla dönen eşlemeyle (rol dizini + baskın sayısı) farklı türler. Eşya depodan ya
        /// da saha rolü dışındaki (ya da uygun olmayan) kişiden alınır; dinlenen köpek ve kişiye bağlı eşya alınmaz; EMP hücresi taşıyan
        /// yuva bozulmaz. "yok"ta da üretilmiş eşya yoksa hiçbir şey yapmaz.
        /// </summary>
        public void EquipGear(Simulation sim)
        {
            if (sim.Board == null || !TakeGear) return;
            var board = sim.Board;
            var open = GearRecipes(sim).Select(r => r.Output).ToList();
            if (open.Count == 0) return;
            var holders = PlanningBoardData.FieldJobs.Select(role => board.People.FirstOrDefault(p => Fit(sim, p) && p.Job == role)).ToList();
            bool usable(BoardEquipment e) => FacilityGear.IsGear(e.Kind) && e.Charges > 0 && e.Rest == 0 && !e.Bound;
            bool free(BoardEquipment e) => usable(e) && (e.OwnerId == null || !holders.Any(h => h != null && h.Id == e.OwnerId)
                || board.Person(e.OwnerId) is var ow && ow != null && !Fit(sim, ow));
            var kinds = Arac == "tek" ? open.Take(1).ToList() : open.Where(k => board.Equipment.Any(e => e.Kind == k && usable(e))).ToList();
            if (kinds.Count == 0) return;
            int turn = sim.S.RaidLog.Count;
            var taken = new HashSet<string>();
            for (int i = 0; i < holders.Count; i++)
            {
                var h = holders[i];
                if (h == null) continue;
                var mine = board.Equipment.FirstOrDefault(e => e.OwnerId == h.Id && PlanningBoardData.EquipmentSlot(e.Kind) == "utility");
                if (mine != null && (mine.Bound || mine.Kind == "emp_cell")) continue;
                BoardEquipment item = null;
                for (int j = 0; j < kinds.Count && item == null; j++)
                {
                    string k = kinds[(i + turn + j) % kinds.Count];
                    if (mine != null && mine.Kind == k && usable(mine) && !taken.Contains(mine.Id)) { item = mine; break; }
                    item = board.Equipment.FirstOrDefault(e => e.Kind == k && free(e) && !taken.Contains(e.Id));
                }
                if (item == null) continue;
                taken.Add(item.Id);
                if (item != mine) sim.EquipResident(item.Id, h.Id);
            }
        }
    }
}
