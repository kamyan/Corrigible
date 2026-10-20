using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    // Çekmeceler yalnız oyuncuya açık ekran modellerini gösterir; komutlar denetleyiciden geçer.
    public sealed class AynaDrawerView : MonoBehaviour
    {
        [SerializeField] GameObject panel, researchPage, armiesPage, modelPage, logPage, helpPage;
        [SerializeField] TMP_Text title, researchSummary, singularityText, armySummary, selectedArmyInfo, productionSummary, modelSummary, logText, helpText;
        [SerializeField] TMP_Text repairText, newArmyText, marchText;
        [SerializeField] Button closeButton, singularityButton, newArmyButton, repairButton, marchButton;
        [SerializeField] Button[] researchButtons = Array.Empty<Button>(), armyButtons = Array.Empty<Button>(), productionButtons = Array.Empty<Button>();
        [SerializeField] TMP_Text[] researchTexts = Array.Empty<TMP_Text>(), armyTexts = Array.Empty<TMP_Text>(), productionTexts = Array.Empty<TMP_Text>(), modelTexts = Array.Empty<TMP_Text>(), featureTexts = Array.Empty<TMP_Text>();
        [SerializeField] AynaTrendGraphic trendGraph;
        [SerializeField] AynaHeatGraphic heatGraph;
        [SerializeField] Color completeColor = new Color(.15f, .75f, .55f, .25f), runningColor = new Color(.08f, .55f, .72f, .3f), availableColor = new Color(.08f, .3f, .4f, .55f), lockedColor = new Color(.035f, .075f, .11f, .75f), selectedColor = new Color(.38f, .24f, .09f, .55f);
        AynaGameController controller;
        bool bound, open;
        int tab, selectedArmy;
        string selectedRegion;
        static readonly string[] titles = { Loc.N("GELİŞİM"), Loc.N("BİRLİKLER"), Loc.N("AYNA’NIN MODELİ"), Loc.N("GÜNLÜK"), Loc.N("REHBER") };
        static readonly OrderKind[] featureOrders = { OrderKind.Expand, OrderKind.Attack, OrderKind.Train, OrderKind.Produce, OrderKind.March, OrderKind.Feint };
        public bool IsOpen => open;
        public int Tab => tab;
        public int SelectedArmyId => selectedArmy;
        public string SelectedRegion { get => selectedRegion; set { if (selectedRegion == value) return; selectedRegion = value; Render(); } }

        public void Bind(AynaGameController game)
        {
            if (bound) return;
            bound = true; controller = game; controller.Changed += Render;
            Click(closeButton, Close);
            Click(singularityButton, () => Submit(new Order { Kind = OrderKind.StartSingularity }));
            Click(newArmyButton, () => Submit(new Order { Kind = OrderKind.NewArmy, Region = selectedRegion }));
            Click(marchButton, () => Submit(new Order { Kind = OrderKind.March, ArmyId = selectedArmy, Region = selectedRegion }));
            Click(repairButton, Repair);
            for (int i = 0; i < researchButtons.Length; i++) { int index = i; Click(researchButtons[i], () => Research(index)); }
            for (int i = 0; i < armyButtons.Length; i++) { int index = i; Click(armyButtons[i], () => SelectArmy(index)); }
            for (int i = 0; i < productionButtons.Length; i++) { int index = i; Click(productionButtons[i], () => Produce(index)); }
            Close();
        }
        void OnDestroy() { if (controller != null) controller.Changed -= Render; }
        public void Open(int value) { tab = Mathf.Clamp(value, 0, 4); open = true; Render(); }
        public void Close() { open = false; if (panel != null) panel.SetActive(false); }
        static void Click(Button button, UnityEngine.Events.UnityAction action) { if (button != null) button.onClick.AddListener(action); }
        static void Text(TMP_Text label, string value) { if (label != null) label.text = value; }
        static void Active(GameObject page, bool value) { if (page != null) page.SetActive(value); }
        static void Enable(Button button, bool value) { if (button != null) button.interactable = value; }
        static void Tint(Button button, Color value) { if (button != null && button.targetGraphic != null) button.targetGraphic.color = value; }
        bool Planning => controller?.Screens?.Phase == AynaPhase.Planning;
        double Remaining => controller?.Screens?.A2 == null ? 0 : Math.Max(0, controller.Screens.A2.Compute - controller.Screens.A2.Cost);
        void Submit(Order order) { if (Planning) controller.Queue(order); }
        void Research(int index)
        {
            var nodes = controller?.Screens?.A8?.Nodes;
            if (nodes == null || index < 0 || index >= nodes.Length) return;
            Submit(new Order { Kind = OrderKind.Train, Family = nodes[index].Family });
        }
        void SelectArmy(int index)
        {
            var armies = controller?.Screens?.A9?.Armies;
            if (armies == null || index < 0 || index >= armies.Length) return;
            selectedArmy = armies[index].Id; Render();
        }
        void Produce(int index)
        {
            var production = controller?.Screens?.A9?.Production;
            if (production == null || index < 0 || index >= production.Length) return;
            Submit(new Order { Kind = OrderKind.Produce, ArmyId = selectedArmy, Unit = production[index].Kind });
        }
        void Repair()
        {
            var s = controller?.Screens; var army = s?.A9?.Armies.FirstOrDefault(a => a.Id == selectedArmy);
            var unit = army?.Units.FirstOrDefault(u => u.Hp < u.MaxHp && !s.A2.Queue.Any(o => o.Kind == OrderKind.Repair && o.UnitId == u.Id));
            if (unit != null) Submit(new Order { Kind = OrderKind.Repair, ArmyId = selectedArmy, UnitId = unit.Id });
        }
        public void Render()
        {
            var s = controller?.Screens;
            if (s == null || s.Phase == AynaPhase.Start || s.A2 == null) { Close(); return; }
            if (selectedRegion == null || !s.A2.Regions.Any(r => r.Id == selectedRegion)) selectedRegion = s.A2.Regions.FirstOrDefault(r => r.Core)?.Id;
            if (panel != null) panel.SetActive(open);
            if (!open) return;
            Text(title, Loc.T(titles[tab]));
            Active(researchPage, tab == 0); Active(armiesPage, tab == 1); Active(modelPage, tab == 2); Active(logPage, tab == 3); Active(helpPage, tab == 4);
            if (tab == 0) RenderResearch(s);
            else if (tab == 1) RenderArmies(s);
            else if (tab == 2) RenderModel(s);
            else if (tab == 3) Text(logText, Log(s));
            else RenderHelp();
        }
        void RenderResearch(AynaScreens s)
        {
            var v = s.A8; bool queued = s.A2.Queue.Any(o => o.Kind == OrderKind.Train);
            Text(researchSummary, Loc.F("{0} kademe eğitildi · {1:0.#} İşlem kullanılabilir\nEğitim kalıcıdır; üretim için veri hattına erişim gerekir.", v.Nodes.Count(n => n.Complete), Remaining));
            for (int i = 0; i < researchButtons.Length; i++)
            {
                bool exists = i < v.Nodes.Length; var button = researchButtons[i]; if (button == null) continue; button.gameObject.SetActive(exists); if (!exists) continue;
                var n = v.Nodes[i]; int nextTier = v.Nodes.Count(x => x.Family == n.Family && x.Complete) + 1;
                bool planned = n.Tier == nextTier && s.A2.Queue.Any(o => o.Kind == OrderKind.Train && o.Family == n.Family);
                string state = n.Complete ? Loc.T("Eğitildi") : n.Running ? Loc.F("Eğitimde · {0}/{1} döngü", n.Progress, n.Duration) : planned ? Loc.T("Planda") : n.Servers < n.Required ? Loc.T("Veri yetmiyor") : n.Available && !queued && Remaining >= n.Cost ? Loc.T("Eğit") : Loc.T("Kilitli");
                if (i < researchTexts.Length) Text(researchTexts[i], Loc.F("{0} · K{1}\n{2}\n{3}/{4} sunucu · {5:0.#} İşlem\n{6} döngü · {7}", AynaText.Family(n.Family), n.Tier, Unlock(n), n.Servers, n.Required, n.Cost, n.Duration, state));
                Enable(button, Planning && n.Available && !queued && Remaining >= n.Cost);
                Tint(button, n.Complete ? completeColor : n.Running || planned ? runningColor : n.Available && !queued && Remaining >= n.Cost ? availableColor : lockedColor);
            }
            bool singularityQueued = s.A2.Queue.Any(o => o.Kind == OrderKind.StartSingularity);
            Text(singularityText, v.SingularityStarted ? Loc.F("TEKİLLİK · {0}/{1} döngü\nVeri bölgelerine erişim kesilirse ilerleme durur.", v.SingularityProgress, v.SingularityRequired) : Loc.F("TEKİLLİK\nK3 aile: {0}/{1} · {2:0.#} İşlem · {3} döngü\nBaşlayınca AYNA’ya ilan edilir.", v.TrainedFamilies, v.SingularityFamilies, v.SingularityCost, v.SingularityRequired));
            Enable(singularityButton, Planning && v.CanStartSingularity && !singularityQueued && Remaining >= v.SingularityCost);
            Tint(singularityButton, v.SingularityStarted || singularityQueued ? runningColor : availableColor);
        }
        static string Unlock(ResearchVm node)
        {
            if (node.Tier < 3) return AynaText.Unit(AynaUnits.All.First(u => u.Family == node.Family && u.Tier == node.Tier).Kind);
            switch (node.Family)
            {
                case DataFamily.Vision: return Loc.T("Uydu bağlantıları ve tuzak görüşü");
                case DataFamily.Movement: return Loc.T("Geniş kadro");
                case DataFamily.Energy: return Loc.T("Güçlü duvarlar");
                case DataFamily.Network: return Loc.T("Geniş yayılım");
                default: return Loc.T("Tahmin ekranını bozma");
            }
        }
        void RenderArmies(AynaScreens s)
        {
            var v = s.A9;
            if (!v.Armies.Any(a => a.Id == selectedArmy)) selectedArmy = (v.Armies.FirstOrDefault(a => a.Region == selectedRegion) ?? v.Armies.FirstOrDefault())?.Id ?? 0;
            var selected = v.Armies.FirstOrDefault(a => a.Id == selectedArmy);
            Text(armySummary, Loc.F("{0}/{1} birlik · {2} birim\nHedef bölge: {3}", v.Armies.Length, v.MaxArmies, v.Armies.Sum(a => a.Units.Length), AynaText.Region(selectedRegion)));
            for (int i = 0; i < armyButtons.Length; i++)
            {
                var button = armyButtons[i]; if (button == null) continue; bool exists = i < v.Armies.Length; button.gameObject.SetActive(exists); if (!exists) continue;
                var a = v.Armies[i]; bool moving = s.A2.Queue.Any(o => (o.Kind == OrderKind.March || o.Kind == OrderKind.Attack) && o.ArmyId == a.Id);
                if (i < armyTexts.Length) Text(armyTexts[i], Loc.F("BİRLİK {0} · {1}\n{2} · {3}/{4} birim\n{5}", a.Id, AynaText.Region(a.Region), moving ? Loc.T("Hareket planda") : Loc.T("Hazır"), a.Units.Length, v.MaxUnits, string.Join(" · ", a.Units.Select(u => AynaText.Unit(u.Kind)))));
                Tint(button, selectedArmy == a.Id ? selectedColor : lockedColor);
            }
            Text(selectedArmyInfo, selected == null ? Loc.T("Önce sahip olduğun bir bölgede yeni birlik kur.") : Loc.F("Seçili birlik {0} · {1}\n{2}", selected.Id, AynaText.Region(selected.Region), string.Join(" · ", selected.Units.Select(u => Loc.F("{0} #{1} {2:0.#}/{3:0.#}", AynaText.Unit(u.Kind), u.Id, u.Hp, u.MaxHp)))));
            bool ownBase = selected != null && s.A2.Regions.Any(r => r.Id == selected.Region && r.Own > 0);
            int pendingUnits = s.A2.Queue.Count(o => o.Kind == OrderKind.Produce && o.ArmyId == selectedArmy);
            bool room = selected != null && selected.Units.Length + pendingUnits < v.MaxUnits;
            for (int i = 0; i < productionButtons.Length; i++)
            {
                var button = productionButtons[i]; if (button == null) continue; bool exists = i < v.Production.Length; button.gameObject.SetActive(exists); if (!exists) continue;
                var p = v.Production[i]; var definition = AynaUnits.Get(p.Kind); bool can = p.Available && ownBase && room && Remaining >= p.Cost;
                if (i < productionTexts.Length) Text(productionTexts[i], Loc.F("{0}\n{1} · K{2} · {3:0.#} İşlem · {4}", AynaText.Unit(p.Kind), AynaText.Family(definition.Family), definition.Tier, p.Cost, !p.Available ? Loc.T("Veri hattı kapalı") : !ownBase ? Loc.T("Dost sunucu gerekli") : !room ? Loc.T("Birlik dolu") : Loc.T("Üret")));
                Enable(button, Planning && can); Tint(button, can ? availableColor : lockedColor);
            }
            Text(productionSummary, Loc.F("Üretim kuyruğu: {0}\nHer birlikte en fazla {1} birim bulunabilir.", string.Join(" · ", s.A2.Queue.Where(o => o.Kind == OrderKind.Produce).Select(o => Loc.F("{0} → Birlik {1}", AynaText.Unit(o.Unit), o.ArmyId))), v.MaxUnits));
            var repair = selected?.Units.FirstOrDefault(u => u.Hp < u.MaxHp && !s.A2.Queue.Any(o => o.Kind == OrderKind.Repair && o.UnitId == u.Id));
            Enable(repairButton, Planning && ownBase && repair != null && Remaining >= v.RepairCost);
            Text(repairText, Loc.F("Onar · {0:0.#} İşlem", v.RepairCost));
            Enable(newArmyButton, Planning && s.A2.Regions.Any(r => r.Id == selectedRegion && r.Own > 0) && v.Armies.Length + s.A2.Queue.Count(o => o.Kind == OrderKind.NewArmy) < v.MaxArmies && Remaining >= v.NewArmyCost);
            Text(newArmyText, Loc.F("Yeni birlik · {0:0.#} İşlem", v.NewArmyCost));
            Enable(marchButton, Planning && selected != null && AynaWorldData.Neighbours(selected.Region, v.CanUseSatellites).Contains(selectedRegion) && !s.A2.Queue.Any(o => (o.Kind == OrderKind.March || o.Kind == OrderKind.Attack) && o.ArmyId == selectedArmy) && Remaining >= v.MarchCost);
            Text(marchText, Loc.F("Yürü → {0} · {1:0.#} İşlem", AynaText.Region(selectedRegion), v.MarchCost));
        }
        void RenderModel(AynaScreens s)
        {
            var v = s.A10;
            Text(modelSummary, Loc.F("AYNA v{0} · {1} tamamlanmış oyun · öğrenme: {2}\nBu döngünün üç tahmini · okunabilirlik %{3:0}", v.Generation, s.A1.Games, AynaText.Level(v.Learning), s.A2.Readability));
            for (int i = 0; i < modelTexts.Length; i++)
                Text(modelTexts[i], i < v.TopThree.Length ? Loc.F("{0}. {1}\n{2}", i + 1, ForecastText.Format(v.TopThree[i]), v.TopThree[i].Confidence >= 1 ? Loc.T("Gözlem desteği yeterli") : Loc.T("Gözlem desteği sınırlı")) : Loc.T("Henüz tahmin yok."));
            double total = v.OrderCounts.Sum();
            for (int i = 0; i < featureTexts.Length; i++)
            {
                if (i >= featureOrders.Length) { Text(featureTexts[i], ""); continue; }
                var kind = featureOrders[i]; double count = v.OrderCounts.ElementAtOrDefault((int)kind);
                Text(featureTexts[i], total > 0 ? Loc.F("{0}\nDoğrulanmış emirlerin %{1:0.#} kadarı\n{2:0.#} gözlem", AynaText.Order(kind), count * 100 / total, count) : Loc.F("{0}\nHenüz gözlem yok.", AynaText.Order(kind)));
            }
            if (trendGraph != null) trendGraph.SetValues(v.Readability);
            if (heatGraph != null) heatGraph.SetValues(v.Heat);
        }
        static string Log(AynaScreens s)
        {
            var lines = s.A3.OwnOrders.Select(r => Loc.F("SEN · {0} · {1} · {2}", AynaText.Order(r.Kind), AynaText.Region(r.Region), AynaText.Rejection(r.Rejection)))
                .Concat(s.A3.EnemySignals.Select(o => o.Verified && o.RevealedOrder.HasValue ? Loc.F("AYNA · {0} · {1} · doğrulandı", AynaText.Order(o.RevealedOrder.Value), AynaText.Region(o.Region)) : Loc.F("SİNYAL · {0} · doğrulanmadı", AynaText.Region(o.Region)))).ToArray();
            return lines.Length == 0 ? Loc.T("Henüz açılmış emir yok. Planını kilitlediğinde iki tarafın sonuçları burada görünür.") : Loc.F("DÖNGÜ {0} · AÇILAN EMİRLER\n\n{1}\n\n{2}", s.A3.Cycle, string.Join("\n\n", lines), s.A3.Surprise ? Loc.T("SÜRPRİZ · Ana taahhüt beklenmiyordu.") : Loc.T("Yalnız doğrulanmış emirler kesin bilgi sayılır."));
        }
        void RenderHelp()
        {
            Text(helpText, Loc.F("{0}\n\n{1}\n\n{2}\n\n{3}\n\n{4}\n\n{5}",
                Loc.T("PLANLA → AÇ → ÇÖZ\nEmirleri aynı plana ekle; plan kilitlendiğinde iki tarafın emirleri birlikte açılır."),
                Loc.T("HARİTA\nBölge seçerek yayıl, birlik yürüt veya saldır. ? bilinmeyen, ≈ tahmin, kesin sayı doğrulanmış bilgidir."),
                Loc.T("GELİŞİM\nSunucular veri ailelerini besler. Eğitim kademeleri kalıcıdır; birim üretmek için ilgili veri hattını korumalısın."),
                Loc.T("BİRLİKLER\nBirlik seç; üretim ve onarım o birliğe uygulanır. Yürü hedefi haritada seçtiğin komşu bölgedir."),
                Loc.T("AYNA’NIN MODELİ\nAYNA doğruladığı davranışlarını öğrenir. Tahminler kesin emir değildir; davranışını değiştirmek modelini yanıltabilir."),
                ForecastText.SurpriseExplanation));
        }
    }
}
