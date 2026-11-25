using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 9: sunucu ödülünün masadaki metinleri ve bekleyen ödül kilidi (Docs/TESIS-BASKINLARI-TASARIMI.md §6.2, §11.5; aşama 9
    /// tasarımı §7.5–7.6). Kararı Core verir (<c>ChooseServerRewardCheck</c>, <c>LiberationRaidCheck</c>); burada yalnız okunur ve yazılır.
    /// Saf: durum değiştirmez, RNG çekmez, komut yazmaz.
    /// <list type="bullet">
    /// <item><see cref="Pending"/>: bekleyen ödülün tek yardımcısı (bayrak, oyun sürüyor, baskın yok, <c>Facility.PendingReward</c> dolu).
    /// Görev fişi ("ÖDÜL SEÇİLMEDİ"), fon kısa uyarısı, CRT, telsiz günlüğü, ödül kâğıdı hep bunu okur; son ekranda (K38)
    /// <c>S.Ending</c> dolu olduğu için hiçbir yüzey bekleyen ödülü göstermez.</item>
    /// <item><see cref="Paper"/>: ödül kâğıdının verisi (sığınak olayı tutanağının görünümü, <see cref="ShelterEventPaperData"/>): iki satır,
    /// TARİF ve KAZANIM.</item>
    /// <item><see cref="GearDesc"/>: yedi ekipmanın tek satırlık açıklaması (sayılar dengeden).</item>
    /// </list>
    /// </summary>
    public static class FacilityRewardText
    {
        /// <summary>Kâğıdın künyesi ve tepsideki etiket başlığı (kaynak metin; <c>DeskTarget.HoverTitle</c> çevirir).</summary>
        public static readonly string TrayTitle = Loc.N("EVRAK · SUNUCU ÖDÜLÜ");
        /// <summary>Kapı panosundaki kâğıdın etiket başlığı (kaynak metin; Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A).</summary>
        public static readonly string DoorTitle = Loc.N("KAPI · SUNUCU ÖDÜLÜ");

        /// <summary>Ödül kâğıdının satır kimlikleri (Core'un seçenek kimlikleri).</summary>
        public static readonly string[] Choices = { FacilityGear.ChoiceRecipe, FacilityGear.ChoicePerk };

        /// <summary>Bekleyen sunucu ödülü var mı: kartlı (panolu) oyun, oyun sürüyor, baskın yok ve
        /// <c>Facility.PendingReward</c> dolu. Core'un ödül kilidi (<c>LiberationRaidCheck</c>) yalnız panolu oyunda işler; tarifler de
        /// panonun defterinde açılır: kartsız oyunda hiçbir yüzey ödülü beklemez.</summary>
        public static bool Pending(Simulation sim) =>
            sim?.B != null && sim.Board != null && sim.S.Ending == null && sim.S.Raid == null &&
            !string.IsNullOrEmpty(sim.Facility?.PendingReward);

        /// <summary>Bekleyen ödülün bölgesi (<see cref="Pending"/> değilse null).</summary>
        public static string Region(Simulation sim) => Pending(sim) ? sim.Facility.PendingReward : null;

        // ------------------------------------------------------------------ ekipman

        /// <summary>Ekipmanın tek satırlık açıklaması (ödül kâğıdı, tarif sayfası); ekipman değilse boş.</summary>
        public static string GearDesc(Balance b, string kind)
        {
            int turns = Math.Max(1, b?.facilityDeviceTurns ?? 3);
            switch (kind)
            {
                case FacilityGear.Wire: return Loc.T("Bitişik boş zemine tel ya da kalas engel kurar.");
                case FacilityGear.Charge: return Loc.T("Bitişik duvarı yıkar; moloz bir kare öteye 1 hasar.");
                case FacilityGear.Turret: return Loc.F("Bir yöne kurulur; her evrenin sonunda hattaki ilk makineye 1 hasar, {0} tur.", turns);
                case FacilityGear.Decoy: return Loc.F("Kurulduğu yönde yürür; makineler onu ekip gibi hedef alır, {0} tur.", turns);
                case FacilityGear.Winch: return Loc.F("Hattaki ilk birimi, düşeni ya da yükü her evrenin sonunda kendine çeker, {0} tur.", turns);
                case FacilityGear.Clamp: return Loc.F("Bitişik robotu {0} tur ele geçirir; kullanılınca tükenir.", Math.Max(1, b?.facilityClampTurns ?? 3));
                case FacilityGear.Dog: return Loc.T("Kendi hamlesi olan dördüncü birim: havla, getir, kokla.");
                default: return string.Empty;
            }
        }

        /// <summary>Ödülle açılacak tarifin bölgesi (ödül tablosunun tersi); tabloda yoksa null.</summary>
        public static string SourceRegion(string recipe)
        {
            foreach (var r in GameData.Regions)
                if (FacilityGear.Reward(r.Id).Recipe == recipe) return r.Id;
            return null;
        }

        /// <summary>Tarifi veren sunucunun YZ adı ("VERITY"; <c>RegionName</c> zaten görünür addır, ikinci kez çevrilmez); yoksa boş.</summary>
        public static string SourceName(Simulation sim, string recipe)
        {
            string region = SourceRegion(recipe);
            return region == null || sim == null ? string.Empty : sim.RegionName(region);
        }

        /// <summary>Tarifin girdileri ve süresi: "2 metal + 1 kumaş · 1 vardiya" (erzak bedeli varsa "… + 4 erzak").</summary>
        public static string InputsLine(BoardRecipe r)
        {
            if (r == null) return string.Empty;
            var parts = r.Inputs.Select(m => Loc.F("{0} {1}", m.Quantity, PlanningBoardSnapshotBuilder.Lowered(PlanningBoardData.MaterialName(m.Kind), Lang))).ToList();
            if (r.Supplies > 0) parts.Add(Loc.F("{0} erzak", DeskFormat.Number(r.Supplies, 0)));
            return Loc.F("{0} · {1}", PlanningBoardSnapshotBuilder.JoinPlus(parts), PlanningBoardSnapshotBuilder.TurnsLine(r.Turns));
        }

        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;

        /// <summary>
        /// Cevap paketinin notu (aşama 8: etkisi aşama 10): temel tarif açık değilse "Temel tarif açık değil: bu harekâtta etkisiz"; Çift
        /// zemberek "Açık olan taret, yem, vinç tariflerinin yerine geçer ({0})". Temel tarifte boş.
        /// </summary>
        public static string AnswerNote(Simulation sim, string recipe)
        {
            if (FacilityGear.IsGear(recipe) || recipe == null) return string.Empty;
            var open = sim?.Facility?.Recipes ?? new List<string>();
            if (recipe == FacilityGear.AnsSpring)
            {
                var devices = new[] { FacilityGear.Turret, FacilityGear.Decoy, FacilityGear.Winch }.Where(open.Contains)
                    .Select(PlanningBoardData.EquipmentName).ToList();
                return devices.Count == 0 ? Loc.T("Temel tarif açık değil: bu harekâtta etkisiz")
                    : Loc.F("Açık olan taret, yem, vinç tariflerinin yerine geçer ({0})", string.Join(", ", devices));
            }
            // Aşama 10: temel Core'dan (FacilityGear.AnswerBases; eski Bridge kopyası kalktı)
            string base_ = FacilityGear.AnswerBases(recipe).FirstOrDefault();
            // Söküm anahtarı Teknisyen kitinin yerine geçer: o tarif baştan açıktır
            if (base_ == "kit_tek") return string.Empty;
            return base_ != null && open.Contains(base_) ? string.Empty : Loc.T("Temel tarif açık değil: bu harekâtta etkisiz");
        }

        // ------------------------------------------------------------------ kâğıt

        /// <summary>
        /// Ödül kâğıdının verisi; bekleyen ödül yoksa (bayrak kapalı, oyun bitti, baskın sürüyor) null. <paramref name="selected"/> ve
        /// <paramref name="denied"/> sunum durumudur (kayda girmez).
        /// </summary>
        public static ShelterEventPaperData Paper(Simulation sim, string selected = null, string denied = null)
        {
            string region = Region(sim);
            if (region == null) return null;
            var (recipe, perk) = FacilityGear.Reward(region);
            if (recipe == null || perk == null) return null;
            var options = sim.ServerRewardOptions(region);
            string building = GameData.Region(region).Building;
            var d = new ShelterEventPaperData
            {
                Key = "odul:" + region,
                Kind = "server_reward",
                Shift = Loc.F("V.{0:000}", sim.S.Day + 1),
                // Künye ("KYGA · SUNUCU ÖDÜLÜ") kâğıdın sahnedeki sabit yazısı; başlık YZ'nin adı (zaten görünür ad), kişi satırı bina
                Title = sim.RegionName(region),
                Person = building,
                Body = Loc.T("Sunucu kanadı alındı. Biri seçilir; öbürü bu harekâtta gelmez."),
            };
            // Satır 1: tarif (temel tarifte açıklama + girdi ve süre; cevap paketinde not)
            var r = FacilityGear.IsGear(recipe) ? FacilityGear.Recipe(sim.B, recipe, sim.Facility.Perks.Contains(FacilityGear.Workbench)) : null;
            string answer = AnswerNote(sim, recipe);
            d.Rows.Add(new ShelterEventRowData
            {
                Id = FacilityGear.ChoiceRecipe, Number = "1", Name = Loc.F("TARİF · {0}", FacilityGear.RecipeName(recipe)),
                // Aşama 10 (K45): önlem defteri açıkken cevabın etkisi (+ "önlem yürürlükte"); kapalıyken aşama 9'un sözcüğü
                Cost = FacilityGear.IsGear(recipe) ? GearDesc(sim.B, recipe) : FacilityCounterText.On(sim) ? FacilityCounterText.AnswerCost(sim, recipe) : Loc.T("Cevap tarifi"),
                Risk = r != null ? InputsLine(r) : string.IsNullOrEmpty(answer) ? null : answer,
                Enabled = options.Contains(FacilityGear.ChoiceRecipe),
            });
            d.Rows.Add(new ShelterEventRowData
            {
                Id = FacilityGear.ChoicePerk, Number = "2", Name = Loc.F("KAZANIM · {0}", FacilityGear.PerkName(perk)),
                Cost = FacilityGear.PerkDesc(sim.B, perk), Enabled = options.Contains(FacilityGear.ChoicePerk),
            });
            foreach (var row in d.Rows) row.Selected = row.Id == selected && row.Enabled;
            var chosen = d.Selected;
            d.ConfirmReady = chosen != null;
            d.Confirm = chosen != null ? Loc.F("{0} · seç", chosen.Name) : Loc.T("Seçmek için 1 ya da 2, sonra Enter");
            // Genel kilit (ör. süren baskın) ya da son ret
            d.Note = denied ?? sim.ChooseServerRewardCheck(region, chosen?.Id ?? FacilityGear.ChoiceRecipe);
            d.HoverLine = Loc.F("{0} · seç", sim.RegionName(region));
            return d;
        }

        /// <summary>Seçilen satırın izi: "SEÇİLDİ" ve seçeneğin adı.</summary>
        public static string[] StampLines(ShelterEventRowData row) => new[] { Loc.T("SEÇİLDİ"), row?.Name ?? string.Empty };

        /// <summary>Tarif seçildiyse teleksin ikinci satırı: "Defterde yeni sayfa: TARİF {0:00}" (sayfa numarası Core'un tarif sırası);
        /// cevap tarifinde (aşama 10) dönüşen sayfa.</summary>
        public static string NewPageLine(Simulation sim, string region)
        {
            string recipe = FacilityGear.Reward(region).Recipe;
            // Aşama 10 (K45): cevap yeni sayfa açmaz, temelin sayfasını dönüştürür: "Defterde TARİF {nn} değişti: {cevap}"
            if (FacilityGear.IsAnswer(recipe)) return FacilityCounterText.ChangedPageLine(sim, recipe);
            int index = sim?.BoardRecipes.FindIndex(x => x.Id == recipe) ?? -1;
            return index < 0 ? null : Loc.F("Defterde yeni sayfa: TARİF {0:00}", index + 1);
        }

        // ------------------------------------------------------------------ rapor

        /// <summary>
        /// Operasyon raporunun SONUÇLAR satırı: baskın bölgeyi tamamladı (<see cref="LiberationRaidReward.ServerReward"/>) ve ödül hâlâ
        /// bekliyor. Yoksa null.
        /// </summary>
        public static OperationRow ReportRow(Simulation sim, RaidLogEntry log)
        {
            string region = log?.LiberationReward?.ServerReward;
            if (string.IsNullOrEmpty(region) || Region(sim) != region) return null;
            return new OperationRow { Label = Loc.T("Sunucu ödülü"), Value = Loc.T("tarif ya da kazanım"), Tone = OperationTone.Good };
        }

        /// <summary>Kazanımların adları (görev fişinin KAZANIM satırı): " · " ile; yoksa boş.</summary>
        public static string PerkList(Simulation sim)
        {
            var perks = sim?.Facility?.Perks;
            return perks == null || perks.Count == 0 ? string.Empty : string.Join(" · ", perks.Select(FacilityGear.PerkName));
        }
    }
}
